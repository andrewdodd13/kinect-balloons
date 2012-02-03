using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BubblesServer
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
            m_receiveStream = m_receiveBuffer.CreateReadStream();
            m_reader = new StreamReader(m_receiveStream, m_encoding);

            m_parsers = new Dictionary<string, MessageParser>();
            m_parsers.Add(NewBalloonMessage.Tag, ParseNewBalloonMessage);
            m_parsers.Add(ChangeScreenMessage.Tag, ParseChangeScreenMessage);
        }
        
        public void Dispose()
        {
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
            Console.WriteLine(">> {0}", line);
            byte[] data = m_encoding.GetBytes(line + "\n");
            m_socket.Send(data);
        }
        #endregion
        #region Implementation
        private Socket m_socket;
        private CircularBuffer m_receiveBuffer;
        private Stream m_receiveStream;
        private StreamReader m_reader;
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
            catch(SocketException)
            {
                ConnectFailed(this, new EventArgs());
                return;
            }
            Connected(this, new EventArgs());
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
            MessageCallback callback = (MessageCallback)result.AsyncState;
            SocketError error;
            int bytesReceived = m_socket.EndReceive(result, out error);
            if(bytesReceived == 0 || error == SocketError.ConnectionReset || error == SocketError.Disconnecting)
            {
                // connection was closed, don't receive any more message
                MessageReceived(this, new MessageEventArgs(null));
                return;
            }
            else if(error != SocketError.Success)
            {
                throw new SocketException((int)error);
            }

            lock(m_receiveBuffer)
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
                    msg = TryReadMessage();
                    if(msg == null)
                    {
                        break;
                    }
                    // move the read cursor forward
                    m_receiveStream.Flush();
                }

                // notify the user that a message was received
                MessageReceived(this, new MessageEventArgs(msg));
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
            int c;
            bool lineFound = false;
            m_receiveStream.Seek(0, SeekOrigin.Begin);
            do
            {
                c = m_receiveStream.ReadByte();
                if((char)c == '\n')
                {
                    lineFound = true;
                    break;
                }
            } while(c >= 0);
            m_receiveStream.Seek(0, SeekOrigin.Begin);
            
            if(!lineFound)
            {
                return null;
            }
            
            // Read the first line
            string line;
            line = m_reader.ReadLine();
            Console.WriteLine("<< {0}", line);
            return ParseMessage(line);
        }
        
        private Message ParseMessage(string line)
        {
            string[] parts = line.Split(' ');
            if(parts.Length < 1)
            {
                throw new Exception("Invalid message: " + line);
            }
            MessageParser parser;
            if(!m_parsers.TryGetValue(parts[0], out parser))
            {
                throw new Exception("Unknown message: " + parts[0]);
            }
            return parser(parts);
        }

        private NewBalloonMessage ParseNewBalloonMessage(string[] parts)
        {
            if(parts.Length != 5)
            {
                throw new Exception("Invalid message");
            }
            int balloonID = Int32.Parse(parts[1]);
            ScreenDirection direction = Screen.ParseDirection(parts[2]);
            Point velocity = new Point();
            velocity.X = Int32.Parse(parts[3]);
            velocity.Y = Int32.Parse(parts[4]);
            return new NewBalloonMessage(balloonID, direction, velocity);
        }
        
        private ChangeScreenMessage ParseChangeScreenMessage(string[] parts)
        {
            if(parts.Length != 3)
            {
                throw new Exception("Invalid message: missing bubble ID or direction");
            }
            int BalloonID = Int32.Parse(parts[1]);
            ScreenDirection direction = Screen.ParseDirection(parts[2]);
            Point velocity = new Point();
            velocity.X = Int32.Parse(parts[3]);
            velocity.Y = Int32.Parse(parts[4]);
            return new ChangeScreenMessage(BalloonID, direction, velocity);
        }
        #endregion
    }
}
