using System;
using System.IO;
using System.Net.Sockets;

namespace BubblesServer
{
    public delegate void ReadCallback(BubblesMessage msg);
    
    internal class ReadOperation
    {
        public byte[] Buffer;
        public ReadCallback Callback;
        
        public ReadOperation(int size, ReadCallback callback)
        {
            Buffer = new byte[size];
            Callback = callback;
        }
    }
    
    public class ScreenConnection
    {
        private Socket m_socket;
        private NetworkStream m_stream;
        private StreamWriter m_writer;
        private MemoryStream m_receiveBuffer;
        private StreamReader m_reader;
        
        
        public ScreenConnection(Socket socket)
        {
            m_socket = socket;
            m_stream = new NetworkStream(m_socket);
            m_writer = new StreamWriter(m_stream);
            m_receiveBuffer = new MemoryStream();
            m_reader = new StreamReader(m_receiveBuffer);
        }
        
        public void BeginReceiveMessage(ReadCallback callback)
        {
            ReadOperation op = new ReadOperation(256, callback);
            m_stream.BeginRead(op.Buffer, 0, op.Buffer.Length, ReadFinished, op);
        }
        
        private void ReadFinished(IAsyncResult result)
        {
            ReadOperation op = (ReadOperation)result.AsyncState;
            int bytesRead = m_stream.EndRead(result);
            if(bytesRead == 0)
            {
                // connection was closed
                op.Callback(null);
                return;
            }
            m_receiveBuffer.Write(op.Buffer, 0, bytesRead);
            
            BubblesMessage msg = TryReadMessage();
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
        
        private BubblesMessage TryReadMessage()
        {
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
            
            string line;
            m_receiveBuffer.Seek(0, SeekOrigin.Begin);
            line = m_reader.ReadLine();
            
            long bytesLeft = m_receiveBuffer.Length - m_receiveBuffer.Position;
            byte[] left = new byte[bytesLeft];
            m_receiveBuffer.Read(left, 0, (int)bytesLeft);
            m_receiveBuffer = new MemoryStream(left);
            
            string[] parts = line.Split(' ');
            if(parts.Length < 1)
            {
                throw new Exception("Invalid message: " + line);
            }
            switch(parts[0])
            {
            case "change-screen":
                return ReadChangeScreenMessage(parts);
            default:
                throw new Exception("Unknown message");
            }
        }
        
        private ChangeScreenMessage ReadChangeScreenMessage(string[] parts)
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
        
        public void SendMessage(BubblesMessage message)
        {
            string line = null;
            switch(message.Type)
            {
            case BubblesMessageType.Add:
                AddMessage am = (AddMessage)message;
                line = string.Format("add {0}", am.BubbleID);
                break;
            default:
                throw new Exception("Unknown message type");
            }
            Console.WriteLine("Sending: {0}", line);
            m_writer.WriteLine(line);
        }
    }
}

