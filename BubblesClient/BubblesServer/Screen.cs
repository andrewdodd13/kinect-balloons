using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace BubblesServer
{
	public class Screen
	{
		// Memebers
		
		private String m_name;
		private Int32 m_id;
		
		private ScreenConnection m_connection;
		private Thread m_thread;
		
		private Queue<Bubble> m_queue;
		
		// Methods
		
		public Screen(String name, Int32 id, Socket socket)
		{
			m_name = name;
			m_id = id;
			m_socket = socket;
			m_thread = new Thread(Run);
			m_thread.Start();
		}
		
		// Getters
		
		public Int32 ID() {
			return m_id;
		}
		
		public String Name() {
			return m_name;
		}
		
		// Thread Main
		
		public void Run() {
			while(true) {
				try {
					BubblesMessage msg = m_connection.ReceiveMessage();
					
					if(msg == null) {	
						break;
					}
					
					switch(msg.Type) {
					case BubblesMessageType.BubbleLeft:
						
						break;
					case BubblesMessageType.Update:
						
						break;
					case BubblesMessageType.Pop:
						
						break;
					}
					
				} catch(ThreadInterruptedException e) {
					ProcessQueue();
				}
			}
		}
		
		// Process new bubbles queue
		public void ProcessQueue() {
			foreach(Bubble b in m_queue) {
				m_connection.SendMessage(new NewBubbleNotification(b));	
			}
		}
		
		
	}
}

