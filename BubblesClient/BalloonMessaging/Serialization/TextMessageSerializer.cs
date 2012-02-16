using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private delegate Message MessageParser(JArray args);
        private delegate void MessageFormatter(JArray args, Message m);
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
            string line = Format(msg);
            if(Configuration.LogNetworkMessages)
            {
                Trace.WriteLine(String.Format(">> {0}", line));
            }
            return m_encoding.GetBytes(line + "\n");
        }

        public string Format(Message msg)
        {
            MessageFormatter formatter;
            if(!m_formatters.TryGetValue(msg.TypeTag, out formatter))
            {
                throw new Exception("Unsupported type: " + msg.TypeTag);
            }
            JArray args = new JArray();
            args.Add(JValue.CreateString(msg.TypeTag));
            formatter(args, msg);
            return args.ToString(Formatting.None);
        }

        private void FormatNewBalloon(JArray args, Message m)
        {
            NewBalloonMessage nbm = (NewBalloonMessage)m;
            args.Add(JValue.FromObject(nbm.BalloonID));
            args.Add(JValue.CreateString(Balloon.FormatDirection(nbm.Direction)));
            args.Add(JValue.FromObject(nbm.Y));
            args.Add(JValue.FromObject(nbm.Velocity.X));
            args.Add(JValue.FromObject(nbm.Velocity.Y));
        }

        private void FormatChangeScreen(JArray args, Message m)
        {
            ChangeScreenMessage csm = (ChangeScreenMessage)m;
            args.Add(JValue.FromObject(csm.BalloonID));
            args.Add(JValue.CreateString(Balloon.FormatDirection(csm.Direction)));
            args.Add(JValue.FromObject(csm.Y));
            args.Add(JValue.FromObject(csm.Velocity.X));
            args.Add(JValue.FromObject(csm.Velocity.Y));
        }

        private void FormatBalloonDecorationUpdate(JArray args, Message m)
        {
            BalloonDecorationUpdateMessage bdm = (BalloonDecorationUpdateMessage)m;
            args.Add(JValue.FromObject(bdm.BalloonID));
            args.Add(JValue.FromObject((int)bdm.OverlayType));
            args.Add(JValue.FromObject(bdm.BackgroundColor.Red));
            args.Add(JValue.FromObject(bdm.BackgroundColor.Green));
            args.Add(JValue.FromObject(bdm.BackgroundColor.Blue));
            args.Add(JValue.FromObject(bdm.BackgroundColor.Alpha));
            args.Add(JValue.FromObject(bdm.Votes));
        }

        private void FormatBalloonContentUpdate(JArray args, Message m)
        {
            BalloonContentUpdateMessage bcm = (BalloonContentUpdateMessage)m;
            args.Add(JValue.FromObject(bcm.BalloonID));
            args.Add(JValue.FromObject((int)bcm.BalloonType));
            args.Add(JValue.CreateString(bcm.Label));
            args.Add(JValue.CreateString(bcm.Content));
            args.Add(JValue.CreateString(bcm.Url));
        }

        private void FormatBalloon(JArray args, Message m)
        {
            BalloonMessage bm = (BalloonMessage)m;
            args.Add(JValue.FromObject(bm.BalloonID));
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
            if(Configuration.LogNetworkMessages)
            {
                Trace.WriteLine(String.Format("<< {0}", line.Substring(0, line.Length - 1)));
            }
            return ParseMessage(line);
        }

        private Message ParseMessage(string line)
        {
            JArray args = JArray.Parse(line);
            string tag = args[0].ToObject<string>();
            MessageParser parser;
            if(!m_parsers.TryGetValue(tag, out parser))
            {
                throw new Exception("Unknown message: " + tag);
            }
            return parser(args);
        }

        private Message ParseNewBalloon(JArray args)
        {
            string balloonID = args[1].ToObject<string>();
            Direction direction = Balloon.ParseDirection(args[2].ToObject<string>());
            float y = args[3].ToObject<float>();
            Vector2D velocity = new Vector2D(args[4].ToObject<float>(), args[5].ToObject<float>());
            return new NewBalloonMessage(balloonID, direction, y, velocity);
        }

        private Message ParseChangeScreen(JArray args)
        {
            string balloonID = args[1].ToObject<string>();
            Direction direction = Balloon.ParseDirection(args[2].ToObject<string>());
            float y = args[3].ToObject<float>();
            Vector2D velocity = new Vector2D(args[4].ToObject<float>(), args[5].ToObject<float>());
            return new ChangeScreenMessage(balloonID, direction, y, velocity);
        }

        private Message ParseBalloonDecorationUpdate(JArray args)
        {
            string balloonID = args[1].ToObject<string>();
            OverlayType overlayType = (OverlayType)args[2].ToObject<int>();
            byte r = args[3].ToObject<byte>();
            byte g = args[4].ToObject<byte>();
            byte b = args[5].ToObject<byte>();
            byte a = args[6].ToObject<byte>();
            Colour c = new Colour(r, g, b, a);
            int votes = args[7].Value<int>();
            return new BalloonDecorationUpdateMessage(balloonID, overlayType, c, votes);
        }

        private Message ParseBalloonContentUpdate(JArray args)
        {
            string balloonID = args[1].ToObject<string>();
            BalloonType balloonType = (BalloonType)args[2].ToObject<int>();
            string label = args[3].ToObject<string>();
            string content = args[4].ToObject<string>();
            string url = args[5].ToObject<string>();
            return new BalloonContentUpdateMessage(balloonID, balloonType, label, content, url);
        }

        private Message ParseBalloon(JArray args)
        {
            string tag = args[0].ToObject<string>();
            string balloonID = args[1].ToObject<string>();
            switch(tag)
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
