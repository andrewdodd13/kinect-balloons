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
        Unknown,
        Left,
        Right
    }
    
	public class Screen
	{
        #region Public interface
        public Screen(string name, int id, ScreenConnection connection, BubblesServer server)
        {
            m_name = name;
            m_id = id;
            m_connection = connection;
            m_server = server;
            m_bubbles = new Dictionary<int, Bubble>();
            m_queue = new ExclusiveCircularQueue<BubblesMessage>(64);
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
        
        /// <summary>
        /// Send a message to the screen. It will be handled in the screen's thread.
        /// </summary>
        public void EnqueueMessage(BubblesMessage message)
        {
            m_queue.Enqueue(message);
        }
        #endregion
        #region Implementation
		// Memebers
		private string m_name;
		private readonly int m_id;
		
        private BubblesServer m_server;
		private ScreenConnection m_connection;
		private Thread m_thread;
		
        private Dictionary<int, Bubble> m_bubbles;
        private ExclusiveCircularQueue<BubblesMessage> m_queue;
        
        /// <summary>
        /// Thread Main.
        /// </summary>
        private void Run()
        {
            Console.WriteLine("New screen: {0}", m_id);
            m_connection.BeginReceiveMessage(MessageReceived);
            try
            {
                while(true)
                {
                    BubblesMessage msg = m_queue.Dequeue();
                    if(msg == null)
                    {    
                        break;
                    }
                    HandleMessage(msg);
                }
            }
            catch(ThreadInterruptedException)
            {
            }
            Console.WriteLine("Screen disconnected: {0}", m_id);
        }
        
        private void HandleMessage(BubblesMessage msg)
        {
            switch(msg.Type)
            {
            case BubblesMessageType.Add:
                AddMessage am = (AddMessage)msg;
                m_bubbles[am.BubbleID] = m_server.GetBubble(am.BubbleID);
                m_connection.SendMessage(am);    
                break;
            case BubblesMessageType.ChangeScreen:
                ChangeScreenMessage csm = (ChangeScreenMessage)msg;
                Screen newScreen = m_server.ChooseNewScreen(this, csm.Direction);
                m_bubbles.Remove(csm.BubbleID);
                m_server.ChangeScreen(csm.BubbleID, newScreen);
                newScreen.EnqueueMessage(new AddMessage(csm.BubbleID));
                break;
            case BubblesMessageType.Update:
             
                break;
            case BubblesMessageType.Pop:
             
                break;
            }
        }
        
        /// <summary>
        /// Called when a message is received from the client (can run in any thread).
        /// </summary>
        private void MessageReceived(BubblesMessage message)
        {
            EnqueueMessage(message);
            if(message != null)
            {
                m_connection.BeginReceiveMessage(MessageReceived);    
            }            
        }
        #endregion
	}
}

