using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework;
using BubblesServer;

namespace DummyClient
{
    public class ScreenManager
    {
        private Socket m_socket;
        private ScreenConnection m_conn;
        private Dictionary<int, Balloon> m_balloons;

        public event EventHandler BalloonMapChanged;

        public Dictionary<int, Balloon> Balloons
        {
            get { return m_balloons; }
        }

        public ScreenManager()
        {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                                  ProtocolType.Tcp);
            m_conn = new ScreenConnection(m_socket);
            m_balloons = new Dictionary<int, Balloon>();
        }

        public void Dispose()
        {
            m_conn.Dispose();
        }

        public void Connect(IPAddress address, int port)
        {
            m_socket.BeginConnect(new IPEndPoint(address, port), OnConnected, null);
        }

        private void OnConnected(IAsyncResult result)
        {
            m_socket.EndConnect(result);
            m_conn.BeginReceiveMessage(OnMessageReceived);
        }

        private void OnMessageReceived(Message msg)
        {
            if(msg.Type == MessageType.Add)
            {
                AddMessage am = (AddMessage)msg;
                Balloon b = new Balloon();
                b.ID = am.BubbleID;
                b.Pos = new Vector2(b.ID * 50, b.ID * 50);
                // TODO synchronize this
                m_balloons.Add(b.ID, b);
                BalloonMapChanged(this, new EventArgs());
            }

            if(msg != null)
            {
                m_conn.BeginReceiveMessage(OnMessageReceived);
            }
        }
    }
}
