using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Balloons.Messaging.Model;

namespace Balloons.Messaging
{
    /// <summary>
    /// Handles the conversion of messages to and from array of bytes.
    /// </summary>
    public class TextMessageSerializer
    {
        private Encoding m_encoding;
        private delegate Message MessageParser(string[] parts);
        private Dictionary<string, MessageParser> m_parsers;

        public TextMessageSerializer()
        {
            m_encoding = Encoding.UTF8;
            m_parsers = new Dictionary<string, MessageParser>();
            m_parsers.Add(NewBalloonMessage.Tag, ParseNewBalloonMessage);
            m_parsers.Add(ChangeScreenMessage.Tag, ParseChangeScreenMessage);
            m_parsers.Add(PopBalloonMessage.Tag, ParsePopBalloonMessage);
        }

        public byte[] Serialize(Message msg)
        {
            string line = msg.Format();
            Debug.WriteLine(">> {0}", line);
            return m_encoding.GetBytes(line + "\n");
        }

        /// <summary>
        /// Tries to read a message from the current buffered data.
        /// </summary>
        /// <returns>
        /// Message read or null if there is not enough data for a complete message.
        /// </returns>
        public Message Deserialize(CircularBuffer buffer)
        {
            // Detect the first newline in the buffered data.
            int lineSize = 0;
            bool lineFound = false;
            while(lineSize < buffer.Available)
            {
                byte c = buffer.PeekByte(lineSize);
                lineSize++;
                if((char)c == '\n')
                {
                    lineFound = true;
                    break;
                }
            }

            if(!lineFound)
            {
                return null;
            }

            // Read the first line
            byte[] messageData = new byte[lineSize];
            buffer.Read(messageData, 0, lineSize);
            string line = m_encoding.GetString(messageData);
            Debug.WriteLine("<< {0}", line.Substring(0, line.Length - 1));
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
            if(parts.Length != 6)
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
            if(parts.Length != 6)
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
            if(parts.Length != 2)
            {
                throw new Exception("Invalid message");
            }
            int balloonID = Int32.Parse(parts[1]);
            return new PopBalloonMessage(balloonID);
        }
    }
}
