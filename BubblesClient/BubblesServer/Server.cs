using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Balloons;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Server
{
	public class Server
	{
        #region Public interface
        public Server(IPAddress address, int port)
        {
            m_address = address;
            m_port = port;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                  ProtocolType.Tcp);
            m_queue = new CircularQueue<Message>(64);
            m_nextScreenID = 0;
            m_nextPlaneID = 0;
            m_screens = new List<Screen>();
            m_bubbles = new Dictionary<string, ServerBalloon>();
            m_planes = new Dictionary<string, ServerPlane>();
            m_feed = new FeedReader(this, Configuration.FeedURL, Configuration.FeedTimeout);
            m_feed.Start();
            
            m_random = new Random();
        }
        
        public static void Main(string[] args)
        {
            // Load the configuration file
            string configPath = "BalloonServer.conf";
            if(args.Length > 1)
            {
                configPath = args[1];
            }
            // If this path doesn't exist, the config file will be created with default values
            Configuration.Load(configPath);

            // Start the server
            Server server = new Server(Configuration.LocalIPAddress, Configuration.LocalPort);
            server.Run();
        }
        
        public void Run()
        {
            using(m_socket)
            {
                // Listen on the given address and port
                m_socket.Bind(new IPEndPoint(m_address, m_port));
                m_socket.Listen(0);
                Trace.WriteLine("Waiting for clients to connect...");
                m_socket.BeginAccept(AcceptCompleted, null);
                while(true)
                {
                    Message msg = m_queue.Dequeue();
                    try
                    {
                        if(!HandleMessage(msg))
                        {    
                            break;
                        }
                    }
                    catch(Exception ex)
                    {
                        Trace.WriteLine(String.Format("Unhandled exception in server thread: {0}", ex.Message));
                        Trace.WriteLine(ex.StackTrace);
                    }
                }
                Trace.WriteLine("Server is stopping.");
            }
        }
        
        /// <summary>
        /// Send a message to the server. It will be handled in the server's thread.
        /// </summary>
        public void EnqueueMessage(Message message)
        {
            m_queue.Enqueue(message);
        }

        /// <summary>
        /// Send a message to the server, changing its sender. It will be handled in the server's thread.
        /// </summary>
        public void EnqueueMessage(Message message, object sender)
        {
            if(message != null && sender != null)
            {
                message.Sender = sender;
            }
            m_queue.Enqueue(message);
        }

        public int ScreenCount
        {
            get
            {
                lock(this)
                {
                    return m_screens.Count;    
                }
            }
        }

        internal ServerBalloon GetBalloon(string BalloonID)
        {
            ServerBalloon b;
            if(m_bubbles.TryGetValue(BalloonID, out b))
            {
                return b;
            }
            return null;
        }

        internal ServerPlane GetPlane(string planeID)
        {
            ServerPlane p;
            if (m_planes.TryGetValue(planeID, out p))
            {
                return p;
            }
            return null;
        }
        
        private Screen GetScreen(int screenID) {
            foreach(Screen v in m_screens) {
                if(screenID == v.ID) {
                    return v;
                }
            }
            return null;
        }

        public Dictionary<string, ServerBalloon> Balloons()
        {
            return m_bubbles;
        }
       
        #endregion
        #region Implementation
        private IPAddress m_address;
        private int m_port;
        private Socket m_socket;
        private CircularQueue<Message> m_queue;
        private List<Screen> m_screens;
        private int m_nextScreenID;
        private Dictionary<string, ServerBalloon> m_bubbles;
        private Dictionary<string, ServerPlane> m_planes;
        private int m_nextPlaneID;
        private FeedReader m_feed;
        
        private Random m_random;
        
        /// <summary>
        /// Handle a message. Must be called from the server's thread.
        /// </summary>
        /// <returns>
        /// True if the message has been handled, false if messages should stop being processed.
        /// </returns>
        private bool HandleMessage(Message msg)
        {
            if(msg == null)
            {
                return false;
            }
            switch(msg.Type)
            {
            case MessageType.Connected:
                return HandleScreenConnected((ConnectedMessage)msg);
            case MessageType.Disconnected:
                return HandleScreenDisconnected((DisconnectedMessage)msg);
            case MessageType.ChangeScreen:
                return HandleChangeScreen((ChangeScreenMessage)msg);
            case MessageType.NewBalloon:
                return HandleNewBalloon((NewBalloonMessage)msg);
            case MessageType.NewPlane:
                return HandleNewPlane((NewPlaneMessage)msg);
            case MessageType.PopObject:
                return HandlePopObject((PopObjectMessage)msg);
            case MessageType.GetBalloonContent:
                return HandleGetBalloonContent((GetBalloonContentMessage)msg);
            case MessageType.GetBalloonState:
                return HandleGetBalloonState((GetBalloonStateMessage)msg);
            case MessageType.BalloonContentUpdate:
                return HandleBalloonContentUpdate((BalloonContentUpdateMessage)msg);
            case MessageType.BalloonStateUpdate:
                return HandleBalloonStateUpdate((BalloonStateUpdateMessage)msg);
            case MessageType.FeedUpdated:
                return HandleFeedUpdated((FeedUpdatedMessage)msg);
            default:
                // warn about unknown messages
                Trace.WriteLine(String.Format("Warning: message type not handled by server: {0}",
                                              msg.Type));
                return true;
            }
        }
        
        private bool HandleScreenConnected(ConnectedMessage msg)
        {
            int screenID = m_nextScreenID++;
            ScreenConnection conn = new ScreenConnection(m_queue, msg.Connection);
            Screen screen = new Screen(screenID, conn);
            conn.Sender = screen;
            conn.StartReceivingMessages();
            m_screens.Add(screen);
            Trace.WriteLine(String.Format("Screen connected: {0}", screenID));
            m_feed.Refresh();
            return true;
        }
        
        private bool HandleScreenDisconnected(DisconnectedMessage msg)
        {
            Screen s = msg.Sender as Screen;
            if(s == null)
            {
                return true;
            }
            Trace.WriteLine(String.Format("Screen disconnected: {0}", s.ID));

            // Gets screen's balloons
            var balloons = BallonsOnScreen(s.ID);
            // Gets left and right screens
            Screen left = GetPreviousScreen(s);
            Screen right = GetNextScreen(s);
            if(left == s || right == s) {
                // if next or previous screen are equal to current screen
                // it means that this was the last screen left
                m_bubbles.Clear();
                m_planes.Clear();
            } else {
                foreach(ServerBalloon balloon in balloons)
                {
                    // Choose randomly between left or right screen
                    int random = m_random.Next(2);
                    Screen newScreen = null;
                    NewBalloonMessage nbm = null;
                    if(random == 0) {
                        newScreen = left;
                        nbm = new NewBalloonMessage(balloon.ID, Direction.Right,
                                                    0.1f, Configuration.VelocityLeft);
                    } else {
                        newScreen = right;
                        nbm = new NewBalloonMessage(balloon.ID, Direction.Left,
                                                    0.1f, Configuration.VelocityRight);
                    }
                    balloon.Screen = newScreen;
                    if(newScreen != null)
                    {
                        newScreen.Connection.SendMessage(nbm);
                    }
                }

                foreach (ServerPlane plane in m_planes.Values)
                {
                    // Choose randomly between left or right screen
                    int random = m_random.Next(2);
                    Screen newScreen = null;
                    NewPlaneMessage npm = null;
                    if (random == 0)
                    {
                        newScreen = left;
                        npm = new NewPlaneMessage(plane.ID, plane.Type, Direction.Right,
                                                    0.1f, Configuration.VelocityLeft, 0.0f);
                    }
                    else
                    {
                        newScreen = right;
                        npm = new NewPlaneMessage(plane.ID, plane.Type, Direction.Left,
                                                    0.1f, Configuration.VelocityRight, 0.0f);
                    }
                    plane.Screen = newScreen;
                    if (newScreen != null)
                    {
                        newScreen.Connection.SendMessage(npm);
                    }
                }
            }
            m_screens.Remove(s);
            return true;
        }
        
        private bool HandleChangeScreen(ChangeScreenMessage csm)
        {
            Screen oldScreen = (Screen)csm.Sender;
            Screen newScreen = ChooseNewScreen(oldScreen, csm.Direction);

            if (newScreen == null)
            {
                return true;
            }

            Direction newDirection = csm.Direction;
            if (csm.Direction == Direction.Left)
            {
                newDirection = Direction.Right;
            }
            else if (csm.Direction == Direction.Right)
            {
                newDirection = Direction.Left;
            }

            if (IsPlane(csm.ObjectID))
            {
                ServerPlane p = GetPlane(csm.ObjectID);
                if (p == null)
                {
                    return true;
                }
                p.Screen = newScreen;
                newScreen.Connection.SendMessage(new NewPlaneMessage(
                    csm.ObjectID, p.Type, newDirection, csm.Y, csm.Velocity, csm.Time)); 
            }
            else
            {
                ServerBalloon b = GetBalloon(csm.ObjectID);
                if (b == null)
                {
                    return true;
                }
                b.Screen = newScreen;
                newScreen.Connection.SendMessage(new NewBalloonMessage(
                    csm.ObjectID, newDirection, csm.Y, csm.Velocity)); 
            }
            return true;
        }
        
        private bool HandleNewBalloon(NewBalloonMessage nbm)
        {
            if(m_bubbles.ContainsKey(nbm.ObjectID)) {
                // Balloon already present !
                Trace.WriteLine(String.Format("Balloon {0} already present!", nbm.ObjectID));
                return true;
            }
            if(m_screens.Count == 0) {
                // No screen to display balloon -- sad
                return true;
            }
   
            ServerBalloon balloon = new ServerBalloon(nbm.ObjectID);
            m_bubbles[nbm.ObjectID] = balloon;
            // choose a random screen
            int screen_idx = m_random.Next(m_screens.Count);

            // Notify screen with new ballon message
            balloon.Screen = m_screens[screen_idx];
            balloon.Screen.Connection.SendMessage(nbm);

            return true;
        }

        private bool HandleNewPlane(NewPlaneMessage npm)
        {
            if (m_planes.ContainsKey(npm.ObjectID))
            {
                // Balloon already present !
                Trace.WriteLine(String.Format("Plane {0} already present!", npm.ObjectID));
                return true;
            }
            if (m_screens.Count == 0)
            {
                // No screen to display balloon -- sad
                return true;
            }

            int numberOfType = 2;
            PlaneType planetype = (PlaneType)m_random.Next(numberOfType);
            ServerPlane plane = new ServerPlane(npm.ObjectID, planetype);
            m_planes[npm.ObjectID] = plane;

            // choose a random screen
            int screen_idx = m_random.Next(m_screens.Count);

            // Notify screen with new plane message
            plane.Screen = m_screens[screen_idx];
            plane.Screen.Connection.SendMessage(npm);

            return true;
        }
        
        private bool HandleGetBalloonContent(GetBalloonContentMessage gbcm)
        {
            ServerBalloon balloon = GetBalloon(gbcm.ObjectID);
            Screen screen = gbcm.Sender as Screen;
            if((balloon != null) && (screen != null))
            {
                screen.Connection.SendMessage(new BalloonContentUpdateMessage(balloon));
            }
            return true;
        }

        private bool HandleGetBalloonState(GetBalloonStateMessage gbdm)
        {
            ServerBalloon balloon = GetBalloon(gbdm.ObjectID);
            Screen screen = gbdm.Sender as Screen;
            if((balloon != null) && (screen != null))
            {
                screen.Connection.SendMessage(new BalloonStateUpdateMessage(balloon));
            }
            return true;
        }

        private bool HandleBalloonContentUpdate(BalloonContentUpdateMessage bcm)
        {
            bcm.UpdateContent(GetBalloon(bcm.ObjectID));
            return true;
        }

        private bool HandleBalloonStateUpdate(BalloonStateUpdateMessage bdm)
        {
            bdm.UpdateState(GetBalloon(bdm.ObjectID));
            return true;
        }

        private bool HandlePopObject(PopObjectMessage pbm)
        {
            if(m_bubbles.ContainsKey(pbm.ObjectID))
            {
                ServerBalloon b = GetBalloon(pbm.ObjectID);
                if((b != null) && (b.Screen != null))
                {
                    if (!(pbm.Sender is Screen))
                    {
                        b.Screen.Connection.SendMessage(pbm);
                    }
                    else
                    {
                        if (m_bubbles.Count <= (Configuration.MinBalloonsPerScreen * m_screens.Count))
                        {
                            m_feed.Refresh();
                        }
                    }
                }
                m_bubbles.Remove(pbm.ObjectID);
            }
            else if (m_planes.ContainsKey(pbm.ObjectID))
            {
                ServerPlane p = GetPlane(pbm.ObjectID);
                if ((p != null) && (p.Screen != null))
                {
                    if (!(pbm.Sender is Screen))
                    {
                        p.Screen.Connection.SendMessage(pbm);
                    }
                }
                m_planes.Remove(pbm.ObjectID);
            }
            return true;
        }
        
        private bool HandleFeedUpdated(FeedUpdatedMessage fm)
        {
            List<FeedContent> fromFeed = fm.FeedItems;
            
            // Gets news balloons to be displayed
            Dictionary<string, ServerBalloon> fromServer = m_bubbles;
            int old = fromServer.Count, popped = 0, added = 0;

            foreach(ServerBalloon b in fromServer.Values)
            {
                // Check if the bubble need to be keept, or deleted
                if(fromFeed.Find(c => c.ContentID == b.ID) == null) {
                    // Pop the balloon in the server not present in the feed
                    EnqueueMessage(new PopObjectMessage(b.ID), fm.Sender);
                    popped++;
                }
            }

            foreach(FeedContent i in fromFeed)
            {
                if(!fromServer.ContainsKey(i.ContentID)) {
                    // Add the new balloon to the server and send content and state
                    EnqueueMessage(new NewBalloonMessage(i.ContentID, Direction.Any,
                        0.2f, Configuration.VelocityLeft), fm.Sender);
                    EnqueueMessage(new BalloonContentUpdateMessage(i.ContentID,
                        (BalloonType)i.Type, i.Title, i.Excerpt, i.URL, i.ImageURL), fm.Sender);
                    EnqueueMessage(new BalloonStateUpdateMessage(i.ContentID, 0,
                        Colour.Parse(i.BalloonColour), i.Votes), fm.Sender);
                    added++;
                }
            }

            // TESTING PURPOSE ! -- TO BE DELETED

            // remove all existing planes
            foreach (ServerPlane plane in m_planes.Values)
            {
                if (plane.Screen != null)
                {
                    plane.Screen.Connection.SendMessage(new PopObjectMessage(plane.ID));
                }
            }
            m_planes.Clear();

            // add a new plane
            string planeID = String.Format("PLANE-{0}", m_nextPlaneID++);
            EnqueueMessage(new PopObjectMessage(planeID));
            EnqueueMessage(new NewPlaneMessage(planeID, PlaneType.BurstBallons, Direction.Right, 0.7f, new Vector2D(0.0f, 0.0f), 0.0f));

            Trace.WriteLine(String.Format("Server had {0} balloons, popped {1}, added {2}", old, popped, added));
            return true;
        }

        private void AcceptCompleted(IAsyncResult result)
        {
            try
            {
                Socket conn = m_socket.EndAccept(result);
                EnqueueMessage(new ConnectedMessage(conn));
                m_socket.BeginAccept(AcceptCompleted, null);
            }
            catch(Exception e)
            {
                Trace.WriteLine(String.Format("Error with accept: {0}", e));
            }
        }

        private List<ServerBalloon> BallonsOnScreen(int screenID)
        {
            Screen s = GetScreen(screenID);
            List<ServerBalloon> ballons = new List<ServerBalloon>();
            foreach (ServerBalloon b in m_bubbles.Values)
            {
                if (b.Screen == s)
                {
                    ballons.Add(b);
                }
            }
            return ballons;
        }

        private Screen GetNextScreen(Screen s) {
            int screen_idx = ScreenIndex(s);
            if(screen_idx == -1)
                return null;
            screen_idx = screen_idx != m_screens.Count - 1 ? screen_idx + 1 : 0;
            return m_screens[screen_idx];
        }
        
        private Screen GetPreviousScreen(Screen s) {
            int screen_idx = ScreenIndex(s);
            if(screen_idx == -1)
                return null;
            screen_idx = screen_idx != 0 ? screen_idx - 1 : m_screens.Count -1;
            return m_screens[screen_idx];
        }
        
        private int ScreenIndex(Screen s) {
            int i = 0;
            foreach(Screen v in m_screens) {
                if(s.ID == v.ID) {
                    return i;
                }
                i++;
            }
            return -1;
        }
        
        private Screen ChooseNewScreen(Screen oldScreen, Direction direction)
        {
            if(direction == Direction.Left) {
                return GetPreviousScreen(oldScreen);
            } else if(direction == Direction.Right) {
                return GetNextScreen(oldScreen);
            } else {
                return null;
            }
        }

        private bool IsPlane(string id)
        {
            return m_planes.ContainsKey(id);
        }
        #endregion
	}
}
