using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace BubblesServer
{
    public enum ScreenDirection
    {
        Unknown,
        Left,
        Right
    }
    
	public class Screen
	{
		// Memebers
		
		private String m_name;
		private Int32 m_id;
		
        private BubblesServer m_server;
		private ScreenConnection m_connection;
		private Thread m_thread;
		
        private Dictionary<int, Bubble> m_bubbles;
		private Queue<BubblesMessage> m_queue;
		
		// Methods
		
		public Screen(String name, Int32 id, ScreenConnection connection, BubblesServer server)
		{
			m_name = name;
			m_id = id;
			m_connection = connection;
            m_server = server;
            m_bubbles = new Dictionary<int, Bubble>();
            m_queue = new Queue<BubblesMessage>();
			m_thread = new Thread(Run);
			m_thread.Start();
		}
		
		// Getters
		
		public Int32 ID()
        {
			return m_id;
		}
		
		public String Name()
        {
			return m_name;
		}
		
		// Thread Main
		
		public void Run()
        {
            Console.WriteLine("New screen: {0}", m_id);
			while(true)
            {
				try
                {
					BubblesMessage msg = m_connection.ReceiveMessage();
					
					if(msg == null)
                    {	
						break;
					}
					
					switch(msg.Type)
                    {
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
                catch(ThreadInterruptedException e)
                {
					ProcessQueue();
				}
			}
		}
		
		// Process new bubbles queue
		public void ProcessQueue()
        {
            lock(m_queue)
            {
                foreach(BubblesMessage m in m_queue)
                {
                    switch(m.Type)
                    {
                    case BubblesMessageType.Add:
                        AddMessage am = (AddMessage)m;
                        m_bubbles[am.BubbleID] = m_server.GetBubble(am.BubbleID);
                        m_connection.SendMessage(am);    
                        break;
                    }
                }
            }
		}
        
        public void EnqueueMessage(BubblesMessage message)
        {
            lock(m_queue)
            {
                m_queue.Enqueue(message);
                m_thread.Interrupt();
            }
        }
	}
}

