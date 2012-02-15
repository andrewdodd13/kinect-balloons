using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Balloons.Messaging;
using Balloons.Messaging.Model;
using BubblesClient.Model;
using Microsoft.Xna.Framework;

namespace BubblesClient
{
    public class ScreenManager : IDisposable
    {
        private ScreenConnection m_conn;
        private IPAddress serverAddress;
        private int serverPort;
        private Dictionary<string, Balloon> balloonCache;

        public CircularQueue<Message> MessageQueue { get; private set; }

        public ScreenManager(IPAddress serverAddress, int serverPort)
        {
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;

            this.balloonCache = new Dictionary<string, Balloon>();
            this.MessageQueue = new CircularQueue<Message>(64);

            m_conn = new ScreenConnection(this.MessageQueue);
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
            if(balloonCache.TryGetValue(balloonID, out balloon))
            {
                return balloon;
            }
            return new Balloon(balloonID) { Label = "Test Label", Content = "Test Content", Type = BalloonType.CustomContent };
        }

        public void UpdateBalloonDetails(Balloon balloon)
        {
            Balloon cachedBalloon = null;
            if(!balloonCache.TryGetValue(balloon.ID, out cachedBalloon))
            {
                cachedBalloon = new Balloon(balloon);
                balloonCache.Add(balloon.ID, cachedBalloon);
            }
            cachedBalloon.OverlayType = balloon.OverlayType;
            cachedBalloon.BackgroundColor = balloon.BackgroundColor;
            m_conn.SendMessage(new BalloonDecorationUpdateMessage(balloon.ID,
                balloon.OverlayType, balloon.BackgroundColor));
        }

        private void OnConnected(object sender, EventArgs args)
        {
        }

        private void OnConnectFailed(object sender, EventArgs args)
        {
            Trace.WriteLine("Could not connect to the server");
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
            switch(msg.Type)
            {
            case MessageType.NewBalloon:
                HandleNewBalloon((NewBalloonMessage)msg);
                break;
            case MessageType.BalloonContentUpdate:
                HandleBalloonContentUpdate((BalloonContentUpdateMessage)msg);
                break;
            case MessageType.BalloonDecorationUpdate:
                HandleBalloonDecorationUpdate((BalloonDecorationUpdateMessage)msg);
                break;
            }
        }

        private void HandleNewBalloon(NewBalloonMessage nbm)
        {
            // ask the server to send the balloon's content
            if(!balloonCache.ContainsKey(nbm.BalloonID))
            {
                balloonCache.Add(nbm.BalloonID, new Balloon(nbm.BalloonID));
                m_conn.SendMessage(new GetBalloonContentMessage(nbm.BalloonID));
            }

            // ask the server to send updated decoration details
            // TODO: only do this if the details have been changed
            m_conn.SendMessage(new GetBalloonDecorationMessage(nbm.BalloonID));
        }

        private void HandleBalloonContentUpdate(BalloonContentUpdateMessage bcm)
        {
            Balloon balloon = null;
            if(balloonCache.TryGetValue(bcm.BalloonID, out balloon))
            {
                balloon.Label = bcm.Label;
                balloon.Content = bcm.Content;
                balloon.Type = bcm.BalloonType;
                balloon.Url = bcm.Url;
            }
        }

        private void HandleBalloonDecorationUpdate(BalloonDecorationUpdateMessage bdm)
        {
            Balloon balloon = null;
            if(balloonCache.TryGetValue(bdm.BalloonID, out balloon))
            {
                balloon.OverlayType = bdm.OverlayType;
                balloon.BackgroundColor = bdm.BackgroundColor;
            }
        }
    }
}
