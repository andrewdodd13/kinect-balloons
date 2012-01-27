using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace BubblesServer
{
	public class BubblesServer
	{
        private int m_port;
        private Socket m_socket;
        private int m_nextScreenID;
        private int m_nextBubbleID;
        private Dictionary<int, Screen> m_screens;
        private Dictionary<int, Bubble> m_bubbles;
        
		public BubblesServer(int port)
		{
            m_port = port;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                  ProtocolType.Tcp);
            m_nextScreenID = 0;
            m_nextBubbleID = 0;
            m_screens = new Dictionary<int, Screen>();
            m_bubbles = new Dictionary<int, Bubble>();
		}
        
        public static void Main(string[] args)
        {
            BubblesServer server =  new BubblesServer(4000);
            server.run();
        }
        
        public void run()
        {
            // Create some bubbles
            for(int i = 0; i < 2; i++)
            {
                CreateBubble();
            }
            
            // Listen on the given port
            m_socket.Bind(new IPEndPoint(IPAddress.Any, m_port));
            m_socket.Listen(0);
            using(m_socket)
            {
                Console.WriteLine("Waiting for clients to connect...");
                while(true)
                {
                    Socket conn = m_socket.Accept();
                    int screenID = m_nextScreenID++;
                    ScreenConnection screenConn = new ScreenConnection(conn);
                    Screen screen = new Screen("Foo", screenID, screenConn, this);
                    m_screens.Add(screenID, screen);
                    lock(m_bubbles)
                    {
                        foreach(int bubbleID in m_bubbles.Keys)
                        {
                            screen.EnqueueMessage(new AddMessage(bubbleID));   
                        }
                    }
                }
            }
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
	}
}

