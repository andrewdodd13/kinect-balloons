using System;
using System.IO;
using System.Net;
using System.Timers;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Balloons;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Server
{
    public class FeedReader
    {
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
            this.m_timer.Elapsed += new ElapsedEventHandler(HandleTimerEvent);
        }

        private void HandleTimerEvent(object source, ElapsedEventArgs e) {
            // Connect to WebServer, gets balloons
            List<FeedContent> fromFeed = GetFeedContents();
            
            // Gets news balloons to be displayed
            Dictionary<string, ServerBalloon> fromServer = m_server.Balloons();

            foreach(KeyValuePair<string, ServerBalloon> i in fromServer)
            {
                ServerBalloon b = i.Value;
                // Check if the bubble need to be keept, or deleted
                if(fromFeed.Find(c => c.ContentID == b.ID) == null) {
                    // Pop the balloon in the server not present in the feed
                    m_server.EnqueueMessage(new PopBalloonMessage(b.ID), this);
                }
            }

            foreach(FeedContent i in fromFeed)
            {
                if(!fromServer.ContainsKey(i.ContentID)) {
                    Colour c = new Colour(byte.Parse(i.BalloonColour.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                                          byte.Parse(i.BalloonColour.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                                          byte.Parse(i.BalloonColour.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                                          byte.Parse(i.BalloonColour.Substring(6, 2), System.Globalization.NumberStyles.HexNumber));
                    // Add the new balloon to the server and send content and decoration
                    m_server.EnqueueMessage(new NewBalloonMessage(i.ContentID, Direction.Any, 0.2f, ServerBalloon.VelocityLeft), this);
                    m_server.EnqueueMessage(new BalloonContentUpdateMessage(i.ContentID, i.Type, i.Title, i.Excerpt, i.URL));
                    m_server.EnqueueMessage(new BalloonDecorationUpdateMessage(i.ContentID, 0, c));
                }
            }
        }
        
        public void Start() {
            m_timer.Enabled = true;
        }

        internal List<FeedContent> GetFeedContents()
        {
            string jsonText = m_client.DownloadString(m_feedUrl);
            return JsonConvert.DeserializeObject<List<FeedContent>>(jsonText);
        }

        public List<ServerBalloon> Balloons()
        {
            return m_balloons;
        }
    }
}

