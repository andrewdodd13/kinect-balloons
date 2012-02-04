using System;
using System.Timers;
using System.Collections.Generic;
using System.Drawing;
using Balloons;
using Balloons.Messaging;

namespace Balloons.Server
{
    public class FeedReader
    {
        private Server m_server;
        private string m_feedUrl;
        private double m_interval;
        private Timer m_timer;
        
        private List<ServerBalloon> m_balloons;
        
        public FeedReader(Server server, string feedUrl, double pullIntervals)
        {
            this.m_server = server;
            this.m_feedUrl = feedUrl;
            this.m_interval = pullIntervals;
            this.m_balloons = new List<ServerBalloon>();
            this.m_timer = new Timer(m_interval);
            this.m_timer.Elapsed += new ElapsedEventHandler(HandleTimerEvent);
        }

        private void SendToServer(Message msg)
        {
            msg.Source = MessageSource.FeedReader;
            msg.Sender = this;
            m_server.EnqueueMessage(msg);
        }
        
        private void HandleTimerEvent(object source, ElapsedEventArgs e) {
            // Connect to WebServer, gets balloons
            Dictionary<int, ServerBalloon> fromFeed = GetFeed();
            
            // Gets news balloons to be displayed
            Dictionary<int, ServerBalloon> fromServer = m_server.Balloons();

            foreach(KeyValuePair<int, ServerBalloon> i in fromServer)
            {
                ServerBalloon b = i.Value;
                // Check if the bubble need to be keept, or deleted
                if(!fromFeed.ContainsKey(b.ID)) {
                    // Pop the balloon in the server not present in the feed
                    SendToServer(new PopBalloonMessage(b.ID));
                }
            }

            foreach(KeyValuePair<int, ServerBalloon> i in fromFeed)
            {
                ServerBalloon b = i.Value;
                if(!fromServer.ContainsKey(b.ID)) {
                    // Add the new balloon to the server
                    SendToServer(new NewBalloonMessage(b.ID, Direction.Any, 0.2f, new Point(10, 0)));
                }
            }
        }
        
        public void Start() {
            m_timer.Enabled = true;
        }

        private Dictionary<int, ServerBalloon> GetFeed()
        {
            return new Dictionary<int, ServerBalloon>();
        }

        public List<ServerBalloon> Balloons()
        {
            return m_balloons;
        }
    }
}

