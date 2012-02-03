using System;
using System.Threading;
using System.Collections.Generic;

namespace BubblesServer
{
    /// <summary>
    /// Used to describe the direction taken by a bubble when it leaves a screen.
    /// </summary>
    public enum ScreenDirection
    {
        Any,
        Left,
        Right
    }
    
	public class Screen
	{
        #region Public interface
        public Screen(string name, int id, ScreenConnection connection, Server server)
        {
            m_name = name;
            m_id = id;
            m_connection = connection;
            m_connection.Disconnected += (sender, args) => EnqueueMessage(null);
            m_connection.MessageReceived += (sender, args) => EnqueueMessage(args.Message);
            m_server = server;
            m_bubbles = new Dictionary<int, Bubble>();
            m_queue = new CircularQueue<Message>(64);
            m_thread = new Thread(Run);
            m_thread.Start();
        }
     
        public int ID
        {
            get { return m_id; }
        }
     
        public string Name
        {
            get { return m_name; }
        }

        public static string FormatDirection(ScreenDirection direction)
        {
            switch (direction)
            {
                case ScreenDirection.Left:
                    return "left";
                case ScreenDirection.Right:
                    return "right";
                case ScreenDirection.Any:
                    return "any";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ScreenDirection ParseDirection(string text)
        {
            switch (text)
            {
                case "left":
                    return ScreenDirection.Left;
                case "right":
                    return ScreenDirection.Right;
                case "any":
                    return ScreenDirection.Any;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Send a message to the screen. It will be handled in the screen's thread.
        /// </summary>
        public void EnqueueMessage(Message message)
        {
            m_queue.Enqueue(message);
        }
        #endregion
        #region Implementation
		// Memebers
		private string m_name;
		private readonly int m_id;
		
        private Server m_server;
		private ScreenConnection m_connection;
		private Thread m_thread;
		
        private Dictionary<int, Bubble> m_bubbles;
        private CircularQueue<Message> m_queue;
        
        /// <summary>
        /// Thread Main.
        /// </summary>
        private void Run()
        {
            Console.WriteLine("Screen connected: {0}", m_id);
            using(m_connection)
            {
                m_connection.StartReceivingMessages();
                try
                {
                    while(true)
                    {
                        Message msg = m_queue.Dequeue();
                        if(!HandleMessage(msg))
                        {    
                            break;
                        }
                    }
                }
                catch(ThreadInterruptedException)
                {
                }
            }
            Console.WriteLine("Screen disconnected: {0}", m_id);
            m_server.EnqueueMessage(new DisconnectedMessage(m_id));
        }
        
        /// <summary>
        /// Handles a message. Must be called from the screen's thread.
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
            switch(msg.Type)
            {
            case MessageType.NewBalloon:
                NewBalloonMessage am = (NewBalloonMessage)msg;
                m_bubbles[am.BalloonID] = m_server.GetBubble(am.BalloonID);
                m_bubbles[am.BalloonID].Screen = this;
                m_connection.SendMessage(am);
                return true;
            case MessageType.ChangeScreen:
                ChangeScreenMessage csm = (ChangeScreenMessage)msg;
                csm.SourceScreen = this;
                m_bubbles.Remove(csm.BalloonID);
                m_server.EnqueueMessage(csm);
                return true;
            case MessageType.PopBalloon:
                PopBalloonMessage pbm = (PopBalloonMessage)msg;               
                m_connection.SendMessage(pbm);  // Notify physical screen
                m_server.EnqueueMessage(pbm);   // Notify server
                return true;
            default:
                // Disconnect when receiving unknown messages
                return false;
            }
        }
        
        public int Size() {
            return m_bubbles.Count;
        }
        
        public Dictionary<int, Bubble> GetBalloons() {
            lock(m_bubbles) {
                return m_bubbles;
            }
        }
        #endregion
	}
}
