using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using Balloons;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Server
{
	public class Screen
	{
        #region Public interface
        public Screen(string name, int id, Socket socket, Server server)
        {
            m_name = name;
            m_id = id;
            m_server = server;
            m_bubbles = new Dictionary<string, ServerBalloon>();
            m_queue = new CircularQueue<Message>(64);
            m_connection = new ScreenConnection(m_queue, socket);
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
        public void EnqueueMessage(Message message)
        {
            m_queue.Enqueue(message);
        }

        /// <summary>
        /// Send a message to the screen, changing its sender. It will be handled in the screen's thread.
        /// </summary>
        public void EnqueueMessage(Message message, object sender)
        {
            if(message != null && sender != null)
            {
                message.Sender = sender;
            }
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

        private Dictionary<string, ServerBalloon> m_bubbles;
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
            m_server.EnqueueMessage(new DisconnectedMessage(m_id), this);
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
            case MessageType.ChangeScreen:
                return HandleChangeScreen((ChangeScreenMessage)msg);
            case MessageType.NewBalloon:
                return HandleNewBalloon((NewBalloonMessage)msg);
            case MessageType.PopBalloon:
                return HandlePopBalloon((PopBalloonMessage)msg);
            case MessageType.GetBalloonContent:
                return HandleGetBalloonContent((GetBalloonContentMessage)msg);
            case MessageType.GetBalloonDecoration:
                return HandleGetBalloonDecoration((GetBalloonDecorationMessage)msg);
            case MessageType.BalloonContentUpdate:
                return HandleBalloonContentUpdate((BalloonContentUpdateMessage)msg);
            case MessageType.BalloonDecorationUpdate:
                return HandleBalloonDecorationUpdate((BalloonDecorationUpdateMessage)msg);
            default:
                // Disconnect when receiving unknown messages
                return false;
            }
        }

        private bool HandleNewBalloon(NewBalloonMessage nbm)
        {
            m_bubbles[nbm.BalloonID] = m_server.GetBalloon(nbm.BalloonID);
            m_bubbles[nbm.BalloonID].Screen = this;
            m_connection.SendMessage(nbm);
            return true;
        }

        private bool HandlePopBalloon(PopBalloonMessage pbm)
        {
            if(pbm.Sender is ScreenConnection)
            {
                m_server.EnqueueMessage(pbm);   // Notify server
            }
            else
            {
                m_connection.SendMessage(pbm);  // Notify physical screen
            }
            return true;
        }

        private bool HandleChangeScreen(ChangeScreenMessage csm)
        {
            m_bubbles.Remove(csm.BalloonID);
            m_server.EnqueueMessage(csm, this);
            return true;
        }

        private bool HandleGetBalloonContent(GetBalloonContentMessage gbcm)
        {
            ServerBalloon b = m_server.GetBalloon(gbcm.BalloonID);
            if(b != null)
            {
                m_connection.SendMessage(new BalloonContentUpdateMessage(
                    b.ID, b.Type, b.Label, b.Content, b.Url));
            }
            return true;
        }

        private bool HandleGetBalloonDecoration(GetBalloonDecorationMessage gbdm)
        {
            ServerBalloon b = m_server.GetBalloon(gbdm.BalloonID);
            if(b != null)
            {
                m_connection.SendMessage(new BalloonDecorationUpdateMessage(
                    b.ID, b.OverlayType, b.BackgroundColor));
            }
            return true;
        }

        private bool HandleBalloonContentUpdate(BalloonContentUpdateMessage bcm)
        {
            if(bcm.Sender is ScreenConnection)
            {
                m_server.EnqueueMessage(bcm);   // Notify server
            }
            return true;
        }

        private bool HandleBalloonDecorationUpdate(BalloonDecorationUpdateMessage bdm)
        {
            if(bdm.Sender is ScreenConnection)
            {
                m_server.EnqueueMessage(bdm);   // Notify server
            }
            return true;
        }
        
        public int Size() {
            return m_bubbles.Count;
        }

        public Dictionary<string, ServerBalloon> GetBalloons()
        {
            return m_bubbles;
        }
        #endregion
	}
}
