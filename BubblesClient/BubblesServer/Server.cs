using System;
using System.Collections.Generic;
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
            m_nextBubbleID = 0;
            m_screens = new Dictionary<int, Screen>();
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
            return oldScreen;
        }
        
        public Bubble CreateBubble()
        {
            lock(m_bubbles)
            {
                int bubbleID = m_nextBubbleID++;
                Bubble b = new Bubble(bubbleID);
                m_bubbles[bubbleID] = b;
                return b;
            }
        }
        
        public Bubble GetBubble(int bubbleID)
        {
            lock(m_bubbles)
            {
                return m_bubbles[bubbleID];
            }
        }
        
        public void ChangeScreen(int bubbleID, Screen newScreen)
        {
            lock(m_bubbles)
            {
                Bubble b = GetBubble(bubbleID);
                b.Screen = newScreen;
            }
        }
        #endregion
        #region Implementation
        private int m_port;
        private Socket m_socket;
        private CircularQueue<Message> m_queue;
        private int m_nextScreenID;
        private int m_nextBubbleID;
        private Dictionary<int, Screen> m_screens;
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
            m_screens.Add(screenID, screen);
            lock(m_bubbles)
            {
                foreach(int bubbleID in m_bubbles.Keys)
                {
                    screen.EnqueueMessage(new AddMessage(bubbleID));   
                }
            }
            return true;
        }
        
        private bool HandleScreenDisconnected(DisconnectedMessage msg)
        {
            m_screens.Remove(msg.ScreenID);
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
