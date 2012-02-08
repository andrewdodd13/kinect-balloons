using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Microsoft.Xna.Framework;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.DummyClient
{
    public class ScreenManager : IDisposable
    {
        private ScreenConnection m_conn;
        private IPAddress serverAddress;
        private int serverPort;

        public event EventHandler<MessageEventArgs> NewBalloonEvent;
        public event EventHandler<MessageEventArgs> PopBalloonEvent;

        public ScreenManager(IPAddress serverAddress, int serverPort)
        {
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;

            m_conn = new ScreenConnection();
            m_conn.Connected += OnConnected;
            m_conn.ConnectFailed += OnConnectFailed;
            m_conn.Disconnected += OnDisconnected;
            m_conn.MessageReceived += OnMessageReceived;
        }

        public void Dispose()
        {
            m_conn.Dispose();
        }

        public void Connect()
        {
            m_conn.Connect(serverAddress, serverPort);
        }

        public void MoveBalloonOffscreen(ClientBalloon balloon, Direction direction, float exitHeight, Vector2 velocity)
        {
            Console.WriteLine("Sending balloon away!");
            // Did we already notify the server that the balloon is off-screen?
            if (balloon.OffScreen)
            {
                return;
            }

            // notify the server that the balloon is moving off-screen
            m_conn.SendMessage(new ChangeScreenMessage(balloon.ID, direction, exitHeight, new Vector2D(velocity.X, velocity.Y)));
            balloon.OffScreen = true;
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
            switch (msg.Type)
            {
                case MessageType.NewBalloon:
                    if (NewBalloonEvent != null) { NewBalloonEvent(this, args); }
                    break;
                case MessageType.PopBalloon:
                    if (PopBalloonEvent != null) { PopBalloonEvent(this, args); }
                    break;
            }
        }
    }
}
