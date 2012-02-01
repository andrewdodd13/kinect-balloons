using System;
using System.IO;
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
        /// Enqueue the reception of a message from the client (non blocking).
        /// </summary>
        /// <param name='callback'>
        /// Function to call when a message is received.
        /// </param>
        public void BeginReceiveMessage(MessageCallback callback)
        {
            byte[] buffer;
            int offset, size;
            lock(m_receiveBuffer)
            {
                buffer = m_receiveBuffer.Buffer;
                offset = m_receiveBuffer.WriteOffset;
                size = m_receiveBuffer.ForwardCapacity;
            }
            if(size == 0)
            {
                throw new InvalidOperationException("ForwardCapacity is nil");
            }
            m_socket.BeginReceive(buffer, offset, size,
                                  SocketFlags.None, ReadFinished, callback);
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
        /// Called when the asynchronous receive operation finishes.
        /// </summary>
        private void ReadFinished(IAsyncResult result)
        {
            MessageCallback callback = (MessageCallback)result.AsyncState;
            SocketError error;
            int bytesReceived = m_socket.EndReceive(result, out error);
            if(bytesReceived == 0 || error == SocketError.ConnectionReset || error == SocketError.Disconnecting)
            {
                // connection was closed
                callback(null);
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
            if(msg == null)
            {
                // we did not receive enough data to read the message, try again
                BeginReceiveMessage(callback);
            }
            else
            {
                // notify the caller that a message was received
                callback(msg);
            }
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
