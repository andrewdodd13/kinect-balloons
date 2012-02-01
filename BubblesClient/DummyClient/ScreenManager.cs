using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Microsoft.Xna.Framework;
using BubblesServer;

namespace DummyClient
{
    public class ScreenManager
    {
        private ScreenConnection m_conn;
        private Dictionary<int, Balloon> m_balloons;

        public event EventHandler BalloonMapChanged;

        public Dictionary<int, Balloon> Balloons
        {
            get { return m_balloons; }
        }

        public ScreenManager()
        {
            m_conn = new ScreenConnection();
            m_conn.Connected += OnConnected;
            m_conn.MessageReceived += OnMessageReceived;
            m_balloons = new Dictionary<int, Balloon>();
        }

        public void Dispose()
        {
            m_conn.Dispose();
        }

        public void Connect(IPAddress address, int port)
        {
            m_conn.Connect(address, port);
        }

        private void OnConnected(object sender, EventArgs args)
        {
        }

        private void OnMessageReceived(object sender, MessageEventArgs args)
        {
            Message msg = args.Message;
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
        }
    }
}
