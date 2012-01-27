using System;
using System.Net.Sockets;

namespace BubblesServer
{
    public class ScreenConnection
    {
        private Socket m_socket;
        
        public ScreenConnection(Socket socket)
        {
            m_socket = socket;
        }
        
        public BubblesMessage ReceiveMessage()
        {
            return null;
        }
        
        public void SendMessage(BubblesMessage message)
        {
        }
    }
}

