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
        private ExclusiveCircularQueue<BubblesMessage> m_queue;
		
		// Methods
		
		public Screen(String name, Int32 id, ScreenConnection connection, BubblesServer server)
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
            m_connection.BeginReceiveMessage(MessageReceived);
			while(true)
            {
                BubblesMessage msg = m_queue.Dequeue();
				try
                {
					if(msg == null)
                    {	
						break;
					}
					
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
                catch(ThreadInterruptedException e)
                {
					break;
				}
			}
		}
        
        public void EnqueueMessage(BubblesMessage message)
        {
            m_queue.Enqueue(message);
        }
        
        private void MessageReceived(BubblesMessage message)
        {
            EnqueueMessage(message);
            if(message != null)
            {
                m_connection.BeginReceiveMessage(MessageReceived);    
            }            
        }
	}
}

