using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Timers;
using Balloons.Messaging;
using Balloons.Messaging.Model;
using BubblesClient.Model;
using Microsoft.Xna.Framework;

namespace BubblesClient.Network
{
    /// <summary>
    /// NetworkManager manages the network connection to the Balloon Server
    /// and is the default implementation of INetworkManager.
    /// </summary>
    public class NetworkManager : INetworkManager
    {
        private ScreenConnection m_conn;
        private IPAddress serverAddress;
        private int serverPort;

        private CircularQueue<Message> messageQueue;

        public NetworkManager(IPAddress serverAddress, int serverPort)
        {
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;
            this.messageQueue = new CircularQueue<Message>(256);

            m_conn = new ScreenConnection(this.messageQueue);
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
            Trace.WriteLine("Sending balloon away!");
            // Did we already notify the server that the balloon is off-screen?
            if (balloon.OffScreen)
            {
                return;
            }

            // notify the server that the balloon is moving off-screen
            m_conn.SendMessage(new ChangeScreenMessage(balloon.ID, direction, exitHeight, new Vector2D(velocity.X, velocity.Y)));
            balloon.OffScreen = true;
        }

        public void MovePlaneOffscreen(ClientPlane plane, Direction direction, float exitHeight, Vector2 velocity, float time)
        {
            Trace.WriteLine("Sending plane away!");
            // Did we already notify the server that the plane is off-screen?
            if (plane.OffScreen)
            {
                return;
            }

            // notify the server that the plane is moving off-screen
            m_conn.SendMessage(new ChangeScreenMessage(plane.ID, direction, exitHeight, new Vector2D(velocity.X, velocity.Y), time));
            plane.OffScreen = true;
        }

        public void NotifyBalloonPopped(ClientBalloon balloon)
        {
            if (balloon.OffScreen)
            {
                return;
            }

            m_conn.SendMessage(new PopObjectMessage(balloon.ID));
            balloon.OffScreen = true;
        }

        public void RequestBalloonContent(string balloonID)
        {
            m_conn.SendMessage(new GetBalloonContentMessage(balloonID));
        }

        public void RequestBalloonState(string balloonID)
        {
            m_conn.SendMessage(new GetBalloonStateMessage(balloonID));
        }

        public void UpdateBalloonState(Balloon balloon)
        {
            m_conn.SendMessage(new BalloonStateUpdateMessage(balloon.ID,
                balloon.OverlayType, balloon.BackgroundColor, balloon.Votes));
        }

        public void ProcessMessages(Dictionary<MessageType, Action<Message>> handlers)
        {
            List<Message> messages = messageQueue.DequeueAll();
            foreach (Message msg in messages)
            {
                if (msg == null)
                {
                    // the connection to the server was closed
                    break;
                }

                // special treatment of callback messages
                if (msg.Type == MessageType.Callback)
                {
                    ((CallbackMessage)msg).Callback();
                }

                // call the corresponding message handler, if any
                Action<Message> handler;
                if (handlers.TryGetValue(msg.Type, out handler))
                {
                    handler(msg);
                }
            }
        }

        private void OnConnected(object sender, EventArgs args)
        {
        }

        private void OnConnectFailed(object sender, EventArgs args)
        {
            Trace.WriteLine(String.Format("Could not connect to the server: {0}", Configuration.RemoteIPAddress));
            Environment.Exit(1);
        }

        private void OnDisconnected(object sender, EventArgs args)
        {
            Trace.WriteLine("Disconnected from the server");
            Environment.Exit(1);
        }
    }
}
