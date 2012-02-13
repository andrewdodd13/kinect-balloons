using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Timers;
using System.Collections.Generic;
using Balloons;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Server
{
    public class FeedReader
    {
        private const int MaxBalloonPerScreen = 10;
        private Server m_server;
        private string m_feedUrl;
        private WebClient m_client;
        private double m_interval;
        private Timer m_timer;
        
        private List<ServerBalloon> m_balloons;
        
        public FeedReader(Server server, string feedUrl, double pullIntervals)
        {
            this.m_server = server;
            this.m_feedUrl = feedUrl;
            this.m_interval = pullIntervals;
            this.m_balloons = new List<ServerBalloon>();
            this.m_client = new WebClient();
            this.m_timer = new Timer(m_interval);
            this.m_timer.Elapsed += (sender, args) => Update();
        }

        private void Update()
        {
            // Connect to WebServer, gets balloons
            List<FeedContent> fromFeed = GetFeedContents();
            
            // Gets news balloons to be displayed
            Dictionary<string, ServerBalloon> fromServer = m_server.Balloons();
            int old = fromServer.Count, popped = 0, added = 0;

            foreach(KeyValuePair<string, ServerBalloon> i in fromServer)
            {
                ServerBalloon b = i.Value;
                // Check if the bubble need to be keept, or deleted
                if(fromFeed.Find(c => c.ContentID == b.ID) == null) {
                    // Pop the balloon in the server not present in the feed
                    m_server.EnqueueMessage(new PopBalloonMessage(b.ID), this);
                    popped++;
                }
            }

            foreach(FeedContent i in fromFeed)
            {
                if(!fromServer.ContainsKey(i.ContentID)) {
                    // Add the new balloon to the server and send content and decoration
                    m_server.EnqueueMessage(new NewBalloonMessage(i.ContentID, Direction.Any, 0.2f, ServerBalloon.VelocityLeft), this);
                    m_server.EnqueueMessage(new BalloonContentUpdateMessage(i.ContentID, i.Type, i.Title, i.Excerpt, i.URL));
                    m_server.EnqueueMessage(new BalloonDecorationUpdateMessage(i.ContentID, Colour.Parse(i.BalloonColour)));
                    added++;
                }
            }

            Debug.WriteLine(String.Format("Server had {0} balloons, popped {1}, added {2}", old, popped, added));
        }
        
        public void Start() {
            m_timer.Enabled = true;
        }

        public void Refresh()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(state => Update());
        }

        internal List<FeedContent> GetFeedContents()
        {
            int numBalloons = m_server.ScreenCount * MaxBalloonPerScreen;
            if(numBalloons == 0)
            {
                return new List<FeedContent>();
            }
            string url = String.Format(m_feedUrl, numBalloons);
            Debug.Write(String.Format("Refreshing feed '{0}' ... ", url));
            string jsonText = m_client.DownloadString(url);
            Debug.Write("done");
            var contents = FeedContent.ParseList(jsonText);
            Debug.WriteLine(String.Format(" -> {0} items", contents.Count));
            return contents;
        }

        public List<ServerBalloon> Balloons()
        {
            return m_balloons;
        }
    }
}

