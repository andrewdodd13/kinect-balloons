using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string FeedUrl = "http://www.macs.hw.ac.uk/~cgw4/balloons/index.php/api/getFeed/{0}";
        private const int FeedTimeout = 1000 * 60 * 1; // 1 min for now

        public Server(int port)
        {
            m_port = port;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                  ProtocolType.Tcp);
            m_queue = new CircularQueue<Message>(64);
            m_nextScreenID = 0;
            m_screens = new List<Screen>();
            m_bubbles = new Dictionary<string, ServerBalloon>();
            m_feed = new FeedReader(this, FeedUrl, FeedTimeout);
            m_feed.Start();
            
            m_random = new Random();
        }
        
        public static void Main(string[] args)
        {
#if DEBUG
            Debug.Listeners.Add(new ConsoleTraceListener());
#endif
            Server server = new Server(4000);
            server.Run();
        }
        
        public void Run()
        {
            using(m_socket)
            {
                // Listen on the given port
                m_socket.Bind(new IPEndPoint(IPAddress.Any, m_port));
                m_socket.Listen(0);
                Console.WriteLine("Waiting for clients to connect...");
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
                        Debug.WriteLine(String.Format("Unhandled exception in server thread: {0}", ex.Message));
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
                Console.WriteLine("Server is stopping.");
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
        private int m_port;
        private Socket m_socket;
        private CircularQueue<Message> m_queue;
        private int m_nextScreenID;
        private List<Screen> m_screens;
        private Dictionary<string, ServerBalloon> m_bubbles;
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
            case MessageType.PopBalloon:
                return HandlePopBalloon((PopBalloonMessage)msg);
            case MessageType.BalloonContentUpdate:
                return HandleBalloonContentUpdate((BalloonContentUpdateMessage)msg);
            case MessageType.BalloonDecorationUpdate:
                return HandleBalloonDecorationUpdate((BalloonDecorationUpdateMessage)msg);
            case MessageType.FeedUpdated:
                return HandleFeedUpdated((FeedUpdatedMessage)msg);
            default:
                // warn about unknown messages
                Debug.WriteLine(String.Format("Warning: message type not handled by server: {0}",
                                              msg.Type));
                return true;
            }
        }
        
        private bool HandleScreenConnected(ConnectedMessage msg)
        {
            int screenID = m_nextScreenID++;
            Screen screen = new Screen("Screen-" + screenID, screenID, msg.Connection, this);
            m_screens.Add(screen);

            m_feed.Refresh();

            return true;
        }
        
        private bool HandleScreenDisconnected(DisconnectedMessage msg)
        {
            Console.WriteLine("Screen disconnected");
            Screen s = GetScreen(msg.ScreenID);

            // Gets screen's balloons
            var balloons = s.GetBalloons();
            // Gets left and right screens
            Screen left = GetPreviousScreen(s);
            Screen right = GetNextScreen(s);
            if(left == s || right == s) {
                // if next or previous screen are equal to current screen
                // it means that this was the last screen left
                m_bubbles.Clear();
            } else {
                foreach(ServerBalloon i in balloons.Values)
                {
                    // Choose randomly between left or right screen
                    int random = m_random.Next(1);
                    if(random == 0) {
                        left.EnqueueMessage(new NewBalloonMessage(i.ID, Direction.Right, 0.1f, ServerBalloon.VelocityLeft), this);
                    } else {
                        right.EnqueueMessage(new NewBalloonMessage(i.ID, Direction.Left, 0.1f, ServerBalloon.VelocityRight), this);
                    }
                }
            }
            m_screens.Remove(s);
            return true;
        }
        
        private bool HandleChangeScreen(ChangeScreenMessage csm) {
            Screen oldScreen = (Screen)csm.Sender;
            Screen newScreen = ChooseNewScreen(oldScreen, csm.Direction);
            ServerBalloon b = GetBalloon(csm.BalloonID);
            if(b == null)
            {
                // balloon was removed and client wasn't notified yet
                return true;
            }
            b.Screen = newScreen;
            Direction newDirection = csm.Direction;
            if(csm.Direction == Direction.Left)
            {
                newDirection = Direction.Right;
            }
            else if(csm.Direction == Direction.Right)
            {
                newDirection = Direction.Left;
            }
            newScreen.EnqueueMessage(new NewBalloonMessage(csm.BalloonID, newDirection, csm.Y, csm.Velocity), this);
            return true;
        }
        
        private bool HandleNewBalloon(NewBalloonMessage nbm)
        {
            if(m_bubbles.ContainsKey(nbm.BalloonID)) {
                // Balloon already present !
                Debug.WriteLine(String.Format("Balloon {0} already present!", nbm.BalloonID));
                return true;
            }
            if(m_screens.Count == 0) {
                // No screen to display balloon -- sad
                return true;
            }

            m_bubbles[nbm.BalloonID] = new ServerBalloon(nbm.BalloonID);
            if(m_screens.Count > 0 ) {
                int screen_idx = m_random.Next(m_screens.Count);
                m_screens[screen_idx].EnqueueMessage(nbm, this);
            } else {
                m_bubbles[nbm.BalloonID].Screen = null;
            }

            return true;
        }

        private bool HandleBalloonContentUpdate(BalloonContentUpdateMessage bcm)
        {
            ServerBalloon b = GetBalloon(bcm.BalloonID);
            if(b != null)
            {
                b.Label = bcm.Label;
                b.Content = bcm.Content;
                b.Type = bcm.BalloonType;
                b.Url = bcm.Url;
            }
            return true;
        }

        private bool HandleBalloonDecorationUpdate(BalloonDecorationUpdateMessage bdm)
        {
            ServerBalloon b = GetBalloon(bdm.BalloonID);
            if(b != null)
            {
                b.OverlayType = bdm.OverlayType;
                b.BackgroundColor = bdm.BackgroundColor;
            }
            return true;
        }

        private bool HandlePopBalloon(PopBalloonMessage pbm) {
            if(m_bubbles.ContainsKey(pbm.BalloonID)) {
                ServerBalloon b = GetBalloon(pbm.BalloonID);
                m_bubbles.Remove(pbm.BalloonID);
                if(b.Screen != null) {
                    b.Screen.EnqueueMessage(pbm, this); // Notify Screen
                }
            }
            return true;
        }
        
        private bool HandleFeedUpdated(FeedUpdatedMessage fm)
        {
            List<FeedContent> fromFeed = fm.FeedItems;
            
            // Gets news balloons to be displayed
            Dictionary<string, ServerBalloon> fromServer = m_bubbles;
            int old = fromServer.Count, popped = 0, added = 0;

            foreach(KeyValuePair<string, ServerBalloon> i in fromServer)
            {
                ServerBalloon b = i.Value;
                // Check if the bubble need to be keept, or deleted
                if(fromFeed.Find(c => c.ContentID == b.ID) == null) {
                    // Pop the balloon in the server not present in the feed
                    EnqueueMessage(new PopBalloonMessage(b.ID), fm.Sender);
                    popped++;
                }
            }

            foreach(FeedContent i in fromFeed)
            {
                if(!fromServer.ContainsKey(i.ContentID)) {
                    // Add the new balloon to the server and send content and decoration
                    EnqueueMessage(new NewBalloonMessage(i.ContentID, Direction.Any,
                        0.2f, ServerBalloon.VelocityLeft), fm.Sender);
                    EnqueueMessage(new BalloonContentUpdateMessage(i.ContentID,
                        (BalloonType)i.Type, i.Title, i.Excerpt, i.URL), fm.Sender);
                    EnqueueMessage(new BalloonDecorationUpdateMessage(i.ContentID, 0,
                        Colour.Parse(i.BalloonColour)), fm.Sender);
                    added++;
                }
            }

            Debug.WriteLine(String.Format("Server had {0} balloons, popped {1}, added {2}", old, popped, added));
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
                Console.WriteLine("Error with accept: {0}", e);
            }
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
        #endregion
	}
}
