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
            m_receiveBuffer = new MemoryStream();
            m_reader = new StreamReader(m_receiveBuffer, m_encoding);
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
            ReceiveOperation op = new ReceiveOperation(256, callback);
            m_socket.BeginReceive(op.Buffer, 0, op.Buffer.Length,
                                  SocketFlags.None, ReadFinished, op);
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
        private MemoryStream m_receiveBuffer;
        private StreamReader m_reader;
        private Encoding m_encoding;
        
        /// <summary>
        /// Holds state for an asynchronous operation to receive a message.
        /// </summary>
        internal class ReceiveOperation
        {
            /// <summary>
            /// Buffer to write data received from the socket.
            /// </summary>
            public byte[] Buffer;
            /// <summary>
            /// Function to call when a complete message has been received.
            /// </summary>
            public MessageCallback Callback;
            
            public ReceiveOperation(int size, MessageCallback callback)
            {
                Buffer = new byte[size];
                Callback = callback;
            }
        }
        
        /// <summary>
        /// Called when the asynchronous receive operation finishes.
        /// </summary>
        private void ReadFinished(IAsyncResult result)
        {
            ReceiveOperation op = (ReceiveOperation)result.AsyncState;
            int bytesRead = m_socket.EndReceive(result);
            if(bytesRead == 0)
            {
                // connection was closed
                op.Callback(null);
                return;
            }
            m_receiveBuffer.Write(op.Buffer, 0, bytesRead);
            
            Message msg = TryReadMessage();
            if(msg == null)
            {
                // we did not receive enough data to read the message
                BeginReceiveMessage(op.Callback);
            }
            else
            {
                op.Callback(msg);
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
            m_receiveBuffer.Seek(0, SeekOrigin.Begin);
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
            m_receiveBuffer.Seek(0, SeekOrigin.Begin);
            line = m_reader.ReadLine();
            
            Console.WriteLine("<< {0}", line);
            
            // Remove that line from the reception buffer
            long bytesLeft = m_receiveBuffer.Length - m_receiveBuffer.Position;
            byte[] left = new byte[bytesLeft];
            m_receiveBuffer.Read(left, 0, (int)bytesLeft);
            m_receiveBuffer = new MemoryStream(left);
            
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
            case "change-screen":
                return ParseChangeScreenMessage(parts);
            default:
                throw new Exception("Unknown message");
            }
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
