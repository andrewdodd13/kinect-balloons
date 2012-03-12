using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
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
        private System.Timers.Timer m_timer;
        private Thread m_thread;
        private CircularQueue<Message> m_queue;
        
        public FeedReader(Server server, string feedUrl, double pullIntervals)
        {
            this.m_server = server;
            this.m_feedUrl = feedUrl;
            this.m_interval = pullIntervals;
            this.m_queue = new CircularQueue<Message>(64);
            this.m_client = new WebClient();
            this.m_timer = new System.Timers.Timer(m_interval);
            this.m_timer.Elapsed += (sender, args) => Refresh();
            this.m_thread = new Thread(Run);
            this.m_thread.Start();
        }
        
        /// <summary>
        /// Start refreshing the feed at regular intervals.
        /// </summary>
        public void Start()
        {
            lock(this)
            {
                m_timer.Enabled = true;    
            }
        }
        
        /// <summary>
        /// Stop the regular feed updates.
        /// </summary>
        public void Stop()
        {
            lock(this)
            {
                m_timer.Enabled = false;    
            }
        }
  
        /// <summary>
        /// Schedule an update of the feed.
        /// </summary>
        public void Refresh()
        {
            EnqueueMessage(new Message(MessageType.RefreshFeed, "refresh-feed"));
        }
        
        /// <summary>
        /// Send a message to the feed reader. It will be handled in the reader's thread.
        /// </summary>
        public void EnqueueMessage(Message message)
        {
            m_queue.Enqueue(message);
        }

        /// <summary>
        /// Send a message to the feed reader, changing its sender. It will be handled in the reader's thread.
        /// </summary>
        public void EnqueueMessage(Message message, object sender)
        {
            if(message != null && sender != null)
            {
                message.Sender = sender;
            }
            m_queue.Enqueue(message);
        }
        
        /// <summary>
        /// Thread Main.
        /// </summary>
        private void Run()
        {
            while(true)
            {
                Message msg = m_queue.Dequeue();
                try
                {
                    if(!HandleMessage(msg))
                    {    
                        break;
                    }
                }
                catch(Exception ex)
                {
                    Trace.WriteLine(String.Format("Unhandled exception in feed thread: {0}", ex.Message));
                    Trace.WriteLine(ex.StackTrace);
                }
            }
        }
        
        /// <summary>
        /// Handles a message. Must be called from the screen reader's thread.
        /// </summary>
        /// <returns>
        /// True if the message has been handled, false if messages should stop being processed.
        /// </returns>
        private bool HandleMessage(Message msg)
        {
            if(msg == null)
            {
                return false;
            }
            else if(msg.Type == MessageType.RefreshFeed)
            {
                DoRefresh();
            }
            else
            {
                // warn about unknown messages
                Trace.WriteLine(String.Format("Warning: message type not handled by feed: {0}",
                                              msg.Type));
            }
            return true;
        }

        private void DoRefresh()
        {
            // Retrieve updated feed contents
            List<FeedContent> fromFeed = GetFeedContents();

            // Notify server of new feed contents if we managed to retrieve any
            if(fromFeed != null)
            {
                // change items of type 2 to items of type 1
                for (int i = 0; i < fromFeed.Count; i++)
                {
                    if (fromFeed[i].Type == 2)
                    {
                        fromFeed[i].Type = 1;
                    }
                }

                m_server.EnqueueMessage(new FeedUpdatedMessage(fromFeed), this);
            }

            // TESTING PURPOSE ! -- TO BE DELETED
            m_server.EnqueueMessage(new PopObjectMessage("PLANE"));
            m_server.EnqueueMessage(new NewPlaneMessage("PLANE", PlaneType.BurstBallons, Direction.Any, 10.0f, new Vector2D(10.0f, 10.0f), 0.0f));
        }

        internal List<FeedContent> GetFeedContents()
        {
            // figure out how many balloons/feed items we want
            int numBalloons = m_server.ScreenCount * Configuration.MaxBalloonsPerScreen;
            if(numBalloons == 0)
            {
                // do not refresh the feed for zero balloons
                return new List<FeedContent>();
            }
            string url = String.Format(m_feedUrl, numBalloons);
            Trace.Write(String.Format("Refreshing feed '{0}' ... ", url));

            // download the JSON-encoded feed items
            string jsonText;
            try
            {
                jsonText = m_client.DownloadString(url);
            }
            catch(WebException we)
            {
                Trace.WriteLine(String.Format("error: {0}.", we.Message));
                return null;
            }

            // convert the JSON data to a list of feed items
            try
            {
                var contents = FeedContent.ParseList(jsonText);
                Trace.WriteLine(String.Format(" done -> {0} items", contents.Count));
                return contents;
            }
            catch(Exception e)
            {
                Trace.WriteLine(String.Format("error: {0}.", e.Message));
                return null;
            }
        }
    }
}
