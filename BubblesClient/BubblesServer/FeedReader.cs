using System;
using System.Timers;
using System.Collections.Generic;
using System.Drawing;

namespace BubblesServer
{
    public class FeedReader
    {
        private Server m_server;
        private string m_feedUrl;
        private double m_interval;
        private Timer m_timer;
        
        private List<Bubble> m_balloons;
        
        public FeedReader(Server server, string feedUrl, double pullIntervals)
        {
            this.m_server = server;
            this.m_feedUrl = feedUrl;
            this.m_interval = pullIntervals;
            this.m_balloons = new List<Bubble>();
            this.m_timer = new Timer(m_interval);
            this.m_timer.Elapsed += new ElapsedEventHandler(HandleTimerEvent);
        }
        
        private void HandleTimerEvent(object source, ElapsedEventArgs e) {
            // Connect to WebServer, gets balloons
            Dictionary<int, Bubble> fromFeed = GetFeed();
            
            // Gets news balloons to be displayed
            Dictionary<int, Bubble> fromServer = m_server.Balloons();
            
            foreach(KeyValuePair<int, Bubble> i in fromServer) {
                Bubble b = i.Value;
                // Check if the bubble need to be keept, or deleted
                if(!fromFeed.ContainsKey(b.ID)) {
                    // Pop the balloon in the server not present in the feed
                    m_server.EnqueueMessage(new PopBalloonMessage(b.ID));
                }
            }
            
            foreach(KeyValuePair<int, Bubble> i in fromFeed) {
                Bubble b = i.Value;
                if(!fromServer.ContainsKey(b.ID)) {
                    // Add the new balloon to the server
                    m_server.EnqueueMessage(new NewBalloonMessage(b.ID, ScreenDirection.Any, 0.2f, new Point(10, 0)));
                }
            }
        }
        
        public void Start() {
            m_timer.Enabled = true;
        }
        
        private Dictionary<int, Bubble> GetFeed() {
            return new Dictionary<int, Bubble>();
        }
        
        public List<Bubble> Balloons() {
            return m_balloons;
        }
    }
}

