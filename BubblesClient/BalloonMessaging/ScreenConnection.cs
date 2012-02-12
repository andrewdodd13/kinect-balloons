using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Balloons.Messaging.Model;
using Balloons.Serialization;

namespace Balloons.Messaging
{
    /// <summary>
    /// Function that can be used to receive messages.
    /// </summary>
    public delegate void MessageCallback(Message msg);

    /// <summary>
    /// Represents a connection between a screen and the bubble server.
    /// </summary>
    public class ScreenConnection : IDisposable
    {
        #region Public interface
        public Socket Socket
        {
            get { return m_socket; }
        }

        public event EventHandler Connected;
        public event EventHandler ConnectFailed;
        public event EventHandler Disconnected;
        public event EventHandler<MessageEventArgs> MessageReceived;

        /// <summary>
        /// Create a new message connection between screen and server.
        /// </summary>
        public ScreenConnection() : this(null)
        {
        }

        /// <summary>
        /// Create a new message connection between screen and server.
        /// </summary>
        /// <param name="receiveQueue"> Temporary queue used to store received messages. </param>
        public ScreenConnection(CircularQueue<Message> receiveQueue) : this(receiveQueue,
            new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
        }

        /// <summary>
        /// Create a new message connection between screen and server.
        /// </summary>
        /// <param name="receiveQueue"> Temporary queue used to store received messages. </param>
        /// <param name="socket"> Socket to use to send and receive messages. </param>
        public ScreenConnection(CircularQueue<Message> receiveQueue, Socket socket)
        {
            m_socket = socket;
            m_receiveQueue = receiveQueue;
            m_receiveBuffer = new CircularBuffer(4096);
            m_serializer = new TextMessageSerializer();
            //m_serializer = new BinaryMessageSerializer();
        }

        public void Dispose()
        {
            lock (m_receiveBuffer)
            {
                m_disposed = true;
            }
            m_socket.Close();
        }

        /// <summary>
        /// Connect to the server (non blocking).
        /// </summary>
        /// <param name="address"> IP address of the server. </param>
        /// <param name="port"> Server port. </param>
        public void Connect(IPAddress address, int port)
        {
            m_socket.BeginConnect(new IPEndPoint(address, port), ConnectedFinished, null);
        }

        /// <summary>
        /// Start receiving messages (non blocking).
        /// </summary>
        public void StartReceivingMessages()
        {
            BeginRead();
        }

        /// <summary>
        /// Send a message to the client (blocking).
        /// </summary>
        public void SendMessage(Message message)
        {
            m_socket.Send(m_serializer.Serialize(message));
        }

        protected virtual void OnConnected()
        {
            EventHandler handler = Connected;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnConnectFailed()
        {
            EventHandler handler = ConnectFailed;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnDisconnected()
        {
            if(m_receiveQueue != null)
            {
                m_receiveQueue.Enqueue(null);
            }

            EventHandler handler = Disconnected;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnMessageReceived(Message msg)
        {
            if(m_receiveQueue != null)
            {
                m_receiveQueue.Enqueue(msg);
            }

            EventHandler<MessageEventArgs> handler = MessageReceived;
            if (handler != null)
            {
                handler(this, new MessageEventArgs(msg));
            }
        }
        #endregion
        #region Implementation
        private Socket m_socket;
        private bool m_disposed;
        private CircularBuffer m_receiveBuffer;
        private IMessageSerializer m_serializer;
        private readonly CircularQueue<Message> m_receiveQueue;

        /// <summary>
        /// Called when the asynchronous connect operation finishes.
        /// </summary>
        private void ConnectedFinished(IAsyncResult result)
        {
            try
            {
                m_socket.EndConnect(result);
            }
            catch (SocketException)
            {
                OnConnectFailed();
                return;
            }
            OnConnected();
            // Start receiving messages
            BeginRead();
        }

        /// <summary>
        /// Start an asynchronous receive operation.
        /// </summary>
        private void BeginRead()
        {
            byte[] buffer;
            int offset, size;
            lock (m_receiveBuffer)
            {
                buffer = m_receiveBuffer.Buffer;
                offset = m_receiveBuffer.WriteOffset;
                size = m_receiveBuffer.ForwardCapacity;
            }
            if (size == 0)
            {
                throw new InvalidOperationException("ForwardCapacity is nil");
            }
            m_socket.BeginReceive(buffer, offset, size, SocketFlags.None, ReadFinished, null);
        }

        /// <summary>
        /// Called when the asynchronous receive operation finishes.
        /// </summary>
        private void ReadFinished(IAsyncResult result)
        {
            bool disposed;
            lock (m_receiveBuffer)
            {
                disposed = m_disposed;
            }
            if (disposed)
            {
                // connection was closed, don't receive any more message
                OnDisconnected();
                return;
            }

            SocketError error;
            int bytesReceived = m_socket.EndReceive(result, out error);
            if (bytesReceived == 0 || error == SocketError.ConnectionReset || error == SocketError.Disconnecting)
            {
                // connection was closed, don't receive any more message
                OnDisconnected();
                return;
            }
            else if (error != SocketError.Success)
            {
                throw new SocketException((int)error);
            }

            lock (m_receiveBuffer)
            {
                // the data was written directly by the socket, move the write cursor forward
                m_receiveBuffer.SkipWrite(bytesReceived);
            }

            Message msg;
            while(true)
            {
                lock(m_receiveBuffer)
                {
                    // try to parse one message from the received data
                    msg = m_serializer.Deserialize(m_receiveBuffer); 
                }

                if(msg == null)
                {
                    // we did not receive enough data to parse this message
                    break;
                }

                // notify the user that a message was received
                msg.Sender = this;
                OnMessageReceived(msg);
            };


            // start receiving more data (end of this message or next message)
            BeginRead();
        }
        #endregion
    }
}
