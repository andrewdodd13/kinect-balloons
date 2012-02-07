﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading;
using Microsoft.Xna.Framework;
using Balloons.Messaging;

namespace Balloons.DummyClient
{
    public class ScreenManager
    {
        private ScreenConnection m_conn;
        private IPAddress m_serverAddress;
        private int m_serverPort;
        private Dictionary<int, ClientBalloon> m_balloons;

        public event EventHandler BalloonMapChanged;

        public Dictionary<int, ClientBalloon> Balloons
        {
            get { return m_balloons; }
        }

        public ScreenManager(IPAddress serverAddress, int serverPort)
        {
            m_serverAddress = serverAddress;
            m_serverPort = serverPort;
            m_conn = new ScreenConnection();
            m_conn.Connected += OnConnected;
            m_conn.ConnectFailed += OnConnectFailed;
            m_conn.Disconnected += OnDisconnected;
            m_conn.MessageReceived += OnMessageReceived;
            m_balloons = new Dictionary<int, ClientBalloon>();
        }

        public void Dispose()
        {
            m_conn.Dispose();
        }

        public void Connect()
        {
            m_conn.Connect(m_serverAddress, m_serverPort);
        }

        public void MoveBalloonOffscreen(ClientBalloon b)
        {
            // Did we already notify the server that the balloon is off-screen?
            if(b.OffScreen)
            {
                return;
            }

            Direction dir;
            if(b.Pos.X < 0.0f)
            {
                dir = Direction.Left;
            }
            else if(b.Pos.X > 1.0f)
            {
                dir = Direction.Right;
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
            ClientBalloon b = new ClientBalloon(am.BalloonID);
            switch(am.Direction)
            {
            case Direction.Any:
                b.Pos = new Vector2(0.5f, 0.2f);
                break;
            case Direction.Left:
                b.Pos = new Vector2(0.0f, am.Y);
                break;
            case Direction.Right:
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
