using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;

namespace BubblesServer
{
	public class Server
	{
        #region Public interface
        public Server(int port)
        {
            m_port = port;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                  ProtocolType.Tcp);
            m_queue = new CircularQueue<Message>(64);
            m_nextScreenID = 0;
            m_nextBalloonID = 0;
            m_screens = new List<Screen>();
            m_bubbles = new Dictionary<int, Bubble>();
        }
        
        public static void Main(string[] args)
        {
            Server server = new Server(4000);
            server.Run();
        }
        
        public void Run()
        {
            // Create some bubbles
            for(int i = 0; i < 2; i++)
            {
                CreateBubble();
            }
            
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
                    if(!HandleMessage(msg))
                    {
                        break;
                    }
                }
                Console.WriteLine("Server is stopping.");
            }
        }
        
        /// <summary>
        /// Send a message to the screen. It will be handled in the server's thread.
        /// </summary>
        public void EnqueueMessage(Message message)
        {
            m_queue.Enqueue(message);
        }
        
        public Screen ChooseNewScreen(Screen oldScreen, ScreenDirection direction)
        {
            lock(m_screens) {
                if(direction == ScreenDirection.Left) {
                    return GetPreviousScreen(oldScreen);
                } else if(direction == ScreenDirection.Right) {
                    return GetNextScreen(oldScreen);
                } else {
                    return null;
                }
            }
        }
        
        public Bubble CreateBubble()
        {
            lock(m_bubbles)
            {
                int BalloonID = m_nextBalloonID++;
                Bubble b = new Bubble(BalloonID);
                m_bubbles[BalloonID] = b;
                return b;
            }
        }
        
        public Bubble GetBubble(int BalloonID)
        {
            lock(m_bubbles)
            {
                return m_bubbles[BalloonID];
            }
        }
        
        private Screen GetScreen(int screenID) {
            lock(m_screens) {
                foreach(Screen v in m_screens) {
                    if(screenID == v.ID) {
                        return v;
                    }
                }
                return null;
            }
        }
        
        private Screen GetNextScreen(Screen s) {
            int screen_idx = ScreenIndex(s);
            if(screen_idx == -1)
                return null;
            screen_idx = screen_idx != m_screens.Count ? screen_idx + 1 : 0;
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
            lock(m_screens) {
                int i = 0;
                foreach(Screen v in m_screens) {
                    if(s.ID == v.ID) {
                        return i;
                    }
                    i++;
                }
                return -1;
            }
        }
        
        public void ChangeScreen(int BalloonID, Screen newScreen)
        {
            lock(m_bubbles)
            {
                Bubble b = GetBubble(BalloonID);
                b.Screen = newScreen;
            }
        }
        #endregion
        #region Implementation
        private int m_port;
        private Socket m_socket;
        private CircularQueue<Message> m_queue;
        private int m_nextScreenID;
        private int m_nextBalloonID;
        private List<Screen> m_screens;      
        private Dictionary<int, Bubble> m_bubbles;
        
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
            default:
                // Disconnect when receiving unknown messages
                return false;
            }
        }
        
        private bool HandleScreenConnected(ConnectedMessage msg)
        {
            ScreenConnection screenConn = new ScreenConnection(msg.Connection);
            int screenID = m_nextScreenID++;
            Screen screen = new Screen("Screen-" + screenID, screenID, screenConn, this);
            m_screens.Add( screen);
            lock(m_bubbles)
            {
                foreach(Bubble b in m_bubbles.Values)
                {
                    ScreenDirection dir;
                    float y;
                    PointF velocity;
                    if((b.ID % 2) == 0)
                    {
                        dir = ScreenDirection.Left;
                        velocity = new PointF(0.1f, 0.0f);
                        y = 0.2f;
                    }
                    else
                    {
                        dir = ScreenDirection.Right;
                        velocity = new PointF(-0.1f, 0.0f);
                        y = 0.1f;
                    }
                    screen.EnqueueMessage(new NewBalloonMessage(b.ID, dir, y, velocity));   
                }
            }
            return true;
        }
        
        private bool HandleScreenDisconnected(DisconnectedMessage msg)
        {
            Screen s = GetScreen(msg.ScreenID);
            m_screens.Remove(s);
            var balloons = s.GetBalloons();
            Screen left = GetPreviousScreen(s);
            Screen right = GetNextScreen(s);
            Random r = new Random();
            foreach(KeyValuePair<int, Bubble> i in balloons) {
                int random = r.Next(1);
                if(random == 0) {
                    i.Value.Screen = left;
                } else {
                    i.Value.Screen = right;
                }
            }
            return true;
        }
        
        private bool HandleChangeScreen(ChangeScreenMessage csm) {
            Screen newScreen = ChooseNewScreen(csm.SourceScreen, csm.Direction);
            ChangeScreen(csm.BalloonID, newScreen);
            newScreen.EnqueueMessage(new NewBalloonMessage(csm.BalloonID, csm.Direction, csm.Y, csm.Velocity));
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
        #endregion
	}
}
