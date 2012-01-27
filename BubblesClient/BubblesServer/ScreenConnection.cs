using System;
using System.IO;
using System.Net.Sockets;

namespace BubblesServer
{
    public class ScreenConnection
    {
        private Socket m_socket;
        private NetworkStream m_stream;
        private StreamReader m_reader;
        private StreamWriter m_writer;
        
        public ScreenConnection(Socket socket)
        {
            m_socket = socket;
            m_stream = new NetworkStream(m_socket);
            m_reader = new StreamReader(m_stream);
            m_writer = new StreamWriter(m_stream);
        }
        
        public BubblesMessage ReceiveMessage()
        {
            string line = m_reader.ReadLine();
            if(line == null)
            {
                return null;
            }
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

