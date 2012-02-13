using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Serialization
{
    /// <summary>
    /// Handles the conversion of messages to and from array of bytes.
    /// </summary>
    public class TextMessageSerializer : IMessageSerializer
    {
        private Encoding m_encoding;
        private delegate Message MessageParser(string[] parts);
        private delegate string MessageFormatter(Message m);
        private Dictionary<string, MessageParser> m_parsers;
        private Dictionary<string, MessageFormatter> m_formatters;

        public TextMessageSerializer()
        {
            m_encoding = Encoding.UTF8;
            m_parsers = new Dictionary<string, MessageParser>();
            m_parsers.Add(NewBalloonMessage.Tag, ParseNewBalloon);
            m_parsers.Add(ChangeScreenMessage.Tag, ParseChangeScreen);
            m_parsers.Add(BalloonContentUpdateMessage.Tag, ParseBalloonContentUpdate);
            m_parsers.Add(BalloonDecorationUpdateMessage.Tag, ParseBalloonDecorationUpdate);
            m_parsers.Add(PopBalloonMessage.Tag, ParseBalloon);
            m_parsers.Add(GetBalloonContentMessage.Tag, ParseBalloon);
            m_parsers.Add(GetBalloonDecorationMessage.Tag, ParseBalloon);

            m_formatters = new Dictionary<string, MessageFormatter>();
            m_formatters.Add(NewBalloonMessage.Tag, FormatNewBalloon);
            m_formatters.Add(ChangeScreenMessage.Tag, FormatChangeScreen);
            m_formatters.Add(BalloonContentUpdateMessage.Tag, FormatBalloonContentUpdate);
            m_formatters.Add(BalloonDecorationUpdateMessage.Tag, FormatBalloonDecorationUpdate);
            m_formatters.Add(PopBalloonMessage.Tag, FormatBalloon);
            m_formatters.Add(GetBalloonContentMessage.Tag, FormatBalloon);
            m_formatters.Add(GetBalloonDecorationMessage.Tag, FormatBalloon);
        }

        public byte[] Serialize(Message msg)
        {
            MessageFormatter formatter;
            if(!m_formatters.TryGetValue(msg.TypeTag, out formatter))
            {
                throw new Exception("Unsupported type: " + msg.TypeTag);
            }
            string line = formatter(msg);
            Debug.WriteLine(String.Format(">> {0}", line));
            return m_encoding.GetBytes(line + "\n");
        }

        private string FormatNewBalloon(Message msg)
        {
            NewBalloonMessage nbm = (NewBalloonMessage)msg;
            return String.Format("{0} {1} {2} {3} {4} {5}",
                nbm.TypeTag, nbm.BalloonID, Balloon.FormatDirection(nbm.Direction),
                nbm.Y, nbm.Velocity.X, nbm.Velocity.Y);
        }

        private string FormatChangeScreen(Message msg)
        {
            ChangeScreenMessage csm = (ChangeScreenMessage)msg;
            return String.Format("{0} {1} {2} {3} {4} {5}",
                csm.TypeTag, csm.BalloonID, Balloon.FormatDirection(csm.Direction),
                csm.Y, csm.Velocity.X, csm.Velocity.Y);
        }

        private string FormatBalloonDecorationUpdate(Message msg)
        {
            throw new NotImplementedException();
        }

        private string FormatBalloonContentUpdate(Message msg)
        {
            throw new NotImplementedException();
        }

        private string FormatBalloon(Message msg)
        {
            BalloonMessage bm = (BalloonMessage)msg;
            return String.Format("{0} {1}", msg.TypeTag, bm.BalloonID);
        }

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
            Debug.WriteLine(String.Format("<< {0}", line.Substring(0, line.Length - 1)));
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

        private Message ParseNewBalloon(string[] parts)
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

        private Message ParseChangeScreen(string[] parts)
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
        
        private Message ParseBalloonDecorationUpdate(string[] parts)
        {
            throw new NotImplementedException();
            /*if(parts.Length != 7)
            {
                throw new Exception("Invalid message");
            }
            int balloonID = Int32.Parse(parts[1]);
            int overlayType = Int32.Parse(parts[2]);
            byte r = Byte.Parse(parts[3]);
            byte g = Byte.Parse(parts[4]);
            byte b = Byte.Parse(parts[5]);
            byte a = Byte.Parse(parts[6]);
            Colour c = new Colour(r, g, b, a);
            return new BalloonDecorationUpdateMessage(balloonID, overlayType, c);
             * */
        }
        
        private Message ParseBalloonContentUpdate(string[] parts)
        {
            throw new NotImplementedException();
            /*if(parts.Length != 6)
            {
                throw new Exception("Invalid message");
            }
            int balloonID = Int32.Parse(parts[1]);
            int balloonType = Int32.Parse(parts[2]);
            string label = parts[3];
            string content = parts[4];
            string url = parts[5];
            return new BalloonContentUpdateMessage(balloonID, balloonType, label, content, url);*/
        }

        private Message ParseBalloon(string[] parts)
        {
            if(parts.Length != 2)
            {
                throw new Exception("Invalid message");
            }
            int balloonID = Int32.Parse(parts[1]);
            switch(parts[0])
            {
            case PopBalloonMessage.Tag:
                return new PopBalloonMessage(balloonID);
            case GetBalloonContentMessage.Tag:
                return new GetBalloonContentMessage(balloonID);
            case GetBalloonDecorationMessage.Tag:
                return new GetBalloonDecorationMessage(balloonID);
            default:
                throw new Exception("Invalid message");
            }
        }
    }
}
