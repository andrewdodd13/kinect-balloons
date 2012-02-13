using System;
using System.Net;
using Balloons.Messaging;
using Balloons.Messaging.Model;
using Microsoft.Xna.Framework;

namespace Balloons.DummyClient
{
    public class ScreenManager : IDisposable
    {
        private ScreenConnection m_conn;
        private IPAddress serverAddress;
        private int serverPort;

        public CircularQueue<Message> MessageQueue { get; private set; }

        public ScreenManager(IPAddress serverAddress, int serverPort)
        {
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;

            this.MessageQueue = new CircularQueue<Message>(64);

            m_conn = new ScreenConnection(this.MessageQueue);
            m_conn.Connected += OnConnected;
            m_conn.ConnectFailed += OnConnectFailed;
            m_conn.Disconnected += OnDisconnected;
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

        public Balloon GetBalloonDetails(string balloonID)
        {
            //b.Type = BalloonType.CustomContent;
            //b.Content = "blahifaeowh foawihf awoifj ewoaifjao wfjawiofo";
            //b.Label = "blah blah blah blah";

            return new Balloon(balloonID) { Label = "Test Label", Content = "Test Content", Type = BalloonType.CustomContent };
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
    }
}
