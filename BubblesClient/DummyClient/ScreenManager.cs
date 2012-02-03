using System;
using System.Collections.Generic;
using System.Drawing;
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
            m_conn.ConnectFailed += OnConnectFailed;
            m_conn.Disconnected += OnDisconnected;
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

        public void MoveBalloonOffscreen(Balloon b)
        {
            // Did we already notify the server that the balloon is off-screen?
            if(b.OffScreen)
            {
                return;
            }

            ScreenDirection dir;
            if(b.Pos.X < 0.0f)
            {
                dir = ScreenDirection.Left;
            }
            else if(b.Pos.Y > 1.0f)
            {
                dir = ScreenDirection.Right;
            }
            else
            {
                // balloon still on screen
                return;
            }

            // notify the server that the balloon is moving off-screen
            m_conn.SendMessage(new ChangeScreenMessage(b.ID, dir, b.Pos.Y,
                new PointF(b.Velocity.X, b.Velocity.Y)));
            b.OffScreen = true;
        }

        private void OnConnected(object sender, EventArgs args)
        {
        }

        private void OnConnectFailed(object sender, EventArgs args)
        {
            Console.WriteLine("Could not connect to the server");
            Environment.Exit(1);
        }

        private void OnDisconnected(object sender, EventArgs args)
        {
            Console.WriteLine("Disconnected from the server");
            Environment.Exit(1);
        }

        private void OnMessageReceived(object sender, MessageEventArgs args)
        {
            Message msg = args.Message;
            switch(msg.Type)
            {
            case MessageType.NewBalloon:
                HandleNewBalloon((NewBalloonMessage)msg);
                break;
            case MessageType.PopBalloon:
                HandlePopBalloon((PopBalloonMessage)msg);
                break;
            }
        }

        private void HandleNewBalloon(NewBalloonMessage am)
        {
            Balloon b = new Balloon();
            b.ID = am.BalloonID;
            switch(am.Direction)
            {
            case ScreenDirection.Any:
                b.Pos = new Vector2(b.ID * 50, b.ID * 50);
                break;
            case ScreenDirection.Left:
                b.Pos = new Vector2(0.0f, am.Y);
                break;
            case ScreenDirection.Right:
                b.Pos = new Vector2(1.0f, am.Y);
                break;
            }
            b.Velocity = new Vector2(am.Velocity.X, am.Velocity.Y);
            // TODO synchronize this
            m_balloons.Add(b.ID, b);
            BalloonMapChanged(this, new EventArgs());
        }

        private void HandlePopBalloon(PopBalloonMessage pbm)
        {
            m_balloons.Remove(pbm.BalloonID);
            BalloonMapChanged(this, new EventArgs());
        }
    }
}
