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
        private Dictionary<string, Balloon> balloonCache;

        private CircularQueue<Message> messageQueue;

        public NetworkManager(IPAddress serverAddress, int serverPort)
        {
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;

            this.balloonCache = new Dictionary<string, Balloon>();
            this.messageQueue = new CircularQueue<Message>(256);

            m_conn = new ScreenConnection(this.messageQueue);
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

        public void NotifyBalloonPopped(ClientBalloon balloon)
        {
            if (balloon.OffScreen)
            {
                return;
            }

            m_conn.SendMessage(new PopBalloonMessage(balloon.ID));
            balloon.OffScreen = true;
        }

        public Balloon GetBalloonDetails(string balloonID)
        {
            Balloon balloon = null;
            if (balloonCache.TryGetValue(balloonID, out balloon))
            {
                return balloon;
            }
            return new Balloon(balloonID) { Label = "Test Label", Content = "Test Content", Type = BalloonType.CustomContent };
        }

        public void UpdateBalloonDetails(Balloon balloon)
        {
            Balloon cachedBalloon = null;
            if (!balloonCache.TryGetValue(balloon.ID, out cachedBalloon))
            {
                cachedBalloon = new Balloon(balloon);
                balloonCache.Add(balloon.ID, cachedBalloon);
            }
            cachedBalloon.OverlayType = balloon.OverlayType;
            cachedBalloon.BackgroundColor = balloon.BackgroundColor;
            cachedBalloon.Votes = balloon.Votes;
            m_conn.SendMessage(new BalloonStateUpdateMessage(balloon.ID,
                balloon.OverlayType, balloon.BackgroundColor, balloon.Votes));
        }

        public void ProcessMessages(Action<NewBalloonMessage> OnNewBalloon, Action<PopBalloonMessage> OnPopBalloon,
            Action<BalloonContentUpdateMessage> OnBalloonContentUpdate, Action<BalloonStateUpdateMessage> OnBalloonStateUpdate)
        {
            List<Message> messages = messageQueue.DequeueAll();
            foreach (Message msg in messages)
            {
                if (msg == null)
                {
                    // the connection to the server was closed
                    break;
                }

                switch (msg.Type)
                {
                    case MessageType.NewBalloon:
                        OnNewBalloon((NewBalloonMessage)msg);
                        break;
                    case MessageType.PopBalloon:
                        OnPopBalloon((PopBalloonMessage)msg);
                        break;
                    case MessageType.BalloonContentUpdate:
                        OnBalloonContentUpdate((BalloonContentUpdateMessage)msg);
                        break;
                    case MessageType.BalloonStateUpdate:
                        OnBalloonStateUpdate((BalloonStateUpdateMessage)msg);
                        break;
                    case MessageType.Callback:
                        var cm = (CallbackMessage)msg;
                        cm.Callback();
                        break;
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

        private void OnMessageReceived(object sender, MessageEventArgs args)
        {
            Message msg = args.Message;
            switch (msg.Type)
            {
                case MessageType.NewBalloon:
                    HandleNewBalloon((NewBalloonMessage)msg);
                    break;
                case MessageType.BalloonContentUpdate:
                    HandleBalloonContentUpdate((BalloonContentUpdateMessage)msg);
                    break;
                case MessageType.BalloonStateUpdate:
                    HandleBalloonStateUpdate((BalloonStateUpdateMessage)msg);
                    break;
            }
        }

        private void HandleNewBalloon(NewBalloonMessage nbm)
        {
            // ask the server to send the balloon's content
            if(!balloonCache.ContainsKey(nbm.ObjectID))
            {
                balloonCache.Add(nbm.ObjectID, new Balloon(nbm.ObjectID));
                m_conn.SendMessage(new GetBalloonContentMessage(nbm.ObjectID));
            }

            // ask the server to send up-to-date state
            // TODO: only do this if the details have been changed
            m_conn.SendMessage(new GetBalloonStateMessage(nbm.ObjectID));
        }

        private void HandleBalloonContentUpdate(BalloonContentUpdateMessage bcm)
        {
            Balloon balloon = null;
            if(balloonCache.TryGetValue(bcm.ObjectID, out balloon))
            {
                balloon.Label = bcm.Label;
                balloon.Content = bcm.Content;
                balloon.Type = bcm.BalloonType;
                balloon.Url = bcm.Url;
                balloon.ImageUrl = bcm.ImageUrl;
            }
        }

        private void HandleBalloonStateUpdate(BalloonStateUpdateMessage bdm)
        {
            Balloon balloon = null;
            if(balloonCache.TryGetValue(bdm.ObjectID, out balloon))
            {
                balloon.OverlayType = bdm.OverlayType;
                balloon.BackgroundColor = bdm.BackgroundColor;
                balloon.Votes = bdm.Votes;
            }
        }
    }
}
