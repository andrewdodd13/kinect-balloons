using System;
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

        /// <summary>
        /// Called when the asynchronous connect operation finishes.
        /// </summary>
        private void ConnectedFinished(IAsyncResult result)
        {
            m_socket.EndConnect(result);
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
            
            Message msg;
            lock(m_receiveBuffer)
            {
                // the data was written directly by the socket, move the write cursor forward
                m_receiveBuffer.SkipWrite(bytesReceived);
                // try to parse one message from the received data
                msg = TryReadMessage();
                // move the read cursor forward
                m_receiveStream.Flush();
            }

            // did we receive enough data to read the message?
            if(msg != null)
            {
                // notify the user that a message was received
                MessageReceived(this, new MessageEventArgs(msg));
            }

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
                c = m_reader.Read();
                if((char)c == '\n')
                {
                    lineFound = true;
                    break;
                }
            } while(c >= 0);
            
            if(!lineFound)
            {
                return null;
            }
            
            // Read the first line
            string line;
            m_receiveStream.Seek(0, SeekOrigin.Begin);
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
            switch(parts[0])
            {
            case "add":
                return ParseAddMessage(parts);
            case "change-screen":
                return ParseChangeScreenMessage(parts);
            default:
                throw new Exception("Unknown message");
            }
        }

        private AddMessage ParseAddMessage(string[] parts)
        {
            if(parts.Length != 2)
            {
                throw new Exception("Invalid message: missing bubble ID");
            }
            int bubbleID = Int32.Parse(parts[1]);
            return new AddMessage(bubbleID);
        }
        
        private ChangeScreenMessage ParseChangeScreenMessage(string[] parts)
        {
            if(parts.Length != 3)
            {
                throw new Exception("Invalid message: missing bubble ID or direction");
            }
            int bubbleID = Int32.Parse(parts[1]);
            ScreenDirection direction = ScreenDirection.Unknown;
            switch(parts[2])
            {
            case "left":
                direction = ScreenDirection.Left;
                break;
            case "right":
                direction = ScreenDirection.Right;
                break;
            default:
                throw new Exception("Invalid direction: " + parts[2]);
            }
            return new ChangeScreenMessage(bubbleID, direction);
        }
        #endregion
    }
}
