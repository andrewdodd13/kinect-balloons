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
        
		public BubblesServer(int port)
		{
            m_port = port;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                  ProtocolType.Tcp);
            m_nextScreenID = 0;
            m_screens = new Dictionary<int, Screen>();
		}
        
        public void run()
        {
            m_socket.Bind(new IPEndPoint(IPAddress.Any, m_port));
            m_socket.Listen(0);
            using(m_socket)
            {
                while(true)
                {
                    Socket conn = m_socket.Accept();
                    int screenID = m_nextScreenID++;
                    ScreenConnection screenConn = new ScreenConnection(conn);
                    Screen screen = new Screen("Foo", screenID, screenConn);
                    m_screens.Add(screenID, conn);
                }
            }
        }
	}
}

