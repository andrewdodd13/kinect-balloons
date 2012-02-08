using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Balloons.Messaging.Model;

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

        public ScreenConnection()
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
        }

        public ScreenConnection(Socket socket)
        {
            m_socket = socket;
            m_encoding = new UTF8Encoding();
            m_receiveBuffer = new CircularBuffer(4096);

            m_parsers = new Dictionary<string, MessageParser>();
            m_parsers.Add(NewBalloonMessage.Tag, ParseNewBalloonMessage);
            m_parsers.Add(ChangeScreenMessage.Tag, ParseChangeScreenMessage);
            m_parsers.Add(PopBalloonMessage.Tag, ParsePopBalloonMessage);
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
            string line = message.Format();
            Debug.WriteLine(">> {0}", line);
            byte[] data = m_encoding.GetBytes(line + "\n");
            m_socket.Send(data);
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
            EventHandler handler = Disconnected;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnMessageReceived(Message msg)
        {
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
        private Encoding m_encoding;

        private delegate Message MessageParser(string[] parts);
        private Dictionary<string, MessageParser> m_parsers;

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
            while (true)
            {
                lock (m_receiveBuffer)
                {
                    // try to parse one message from the received data
                    msg = TryReadMessage();
                    if (msg == null)
                    {
                        break;
                    }
                }

                // notify the user that a message was received
                msg.Sender = this;
                OnMessageReceived(msg);
            };


            // start receiving more data (end of this message or next message)
            BeginRead();
        }

        /// <summary>
        /// Tries to read a message from the current buffered data.
        /// </summary>
        /// <returns>
        /// Message read or null if there is not enough data for a complete message.
        /// </returns>
        private Message TryReadMessage()
        {
            // Detect the first newline in the buffered data.
            int lineSize = 0;
            bool lineFound = false;
            while (lineSize < m_receiveBuffer.Available)
            {
                byte c = m_receiveBuffer.PeekByte(lineSize);
                lineSize++;
                if ((char)c == '\n')
                {
                    lineFound = true;
                    break;
                }
            }

            if (!lineFound)
            {
                return null;
            }

            // Read the first line
            byte[] messageData = new byte[lineSize];
            m_receiveBuffer.Read(messageData, 0, lineSize);
            string line = m_encoding.GetString(messageData);
            Debug.WriteLine("<< {0}", line.Substring(0, line.Length - 1));
            return ParseMessage(line);
        }

        private Message ParseMessage(string line)
        {
            string[] parts = line.Split(' ');
            if (parts.Length < 1)
            {
                throw new Exception("Invalid message: " + line);
            }
            MessageParser parser;
            if (!m_parsers.TryGetValue(parts[0], out parser))
            {
                throw new Exception("Unknown message: " + parts[0]);
            }
            return parser(parts);
        }

        private NewBalloonMessage ParseNewBalloonMessage(string[] parts)
        {
            if (parts.Length != 6)
            {
                throw new Exception("Invalid message");
            }
            int balloonID = Int32.Parse(parts[1]);
            Direction direction = Balloon.ParseDirection(parts[2]);
            float y = Single.Parse(parts[3]);
            Vector2D velocity = new Vector2D(Single.Parse(parts[4]), Single.Parse(parts[5]));
            return new NewBalloonMessage(balloonID, direction, y, velocity);
        }

        private ChangeScreenMessage ParseChangeScreenMessage(string[] parts)
        {
            if (parts.Length != 6)
            {
                throw new Exception("Invalid message");
            }
            int balloonID = Int32.Parse(parts[1]);
            Direction direction = Balloon.ParseDirection(parts[2]);
            float y = Single.Parse(parts[3]);
            Vector2D velocity = new Vector2D(Single.Parse(parts[4]), Single.Parse(parts[5]));
            return new ChangeScreenMessage(balloonID, direction, y, velocity);
        }

        private PopBalloonMessage ParsePopBalloonMessage(string[] parts)
        {
            if (parts.Length != 2)
            {
                throw new Exception("Invalid message");
            }
            int balloonID = Int32.Parse(parts[1]);
            return new PopBalloonMessage(balloonID);
        }
        #endregion
    }
}
