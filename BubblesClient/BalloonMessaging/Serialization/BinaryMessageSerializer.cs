using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Serialization
{
    public class BinaryMessageSerializer : IMessageSerializer
    {
        private delegate Message MessageDecoder(BinaryReader reader, MessageType type);
        private delegate void MessageEncoder(BinaryWriter writer, Message msg);

        private Dictionary<MessageType, MessageDecoder> m_decoders;
        private Dictionary<MessageType, MessageEncoder> m_encoders;
        private TextMessageSerializer m_logFormatter;

        public BinaryMessageSerializer()
        {
            m_decoders = new Dictionary<MessageType, MessageDecoder>();
            m_decoders.Add(MessageType.NewBalloon, DecodeNewBalloon);
            m_decoders.Add(MessageType.ChangeScreen, DecodeChangeScreen);
            m_decoders.Add(MessageType.BalloonContentUpdate, DecodeBalloonContentUpdate);
            m_decoders.Add(MessageType.BalloonDecorationUpdate, DecodeBalloonDecorationUpdate);
            m_decoders.Add(MessageType.PopBalloon, DecodeBalloon);
            m_decoders.Add(MessageType.GetBalloonContent, DecodeBalloon);
            m_decoders.Add(MessageType.GetBalloonDecoration, DecodeBalloon);

            m_encoders = new Dictionary<MessageType, MessageEncoder>();
            m_encoders.Add(MessageType.NewBalloon, SerializeNewBalloon);
            m_encoders.Add(MessageType.ChangeScreen, SerializeChangeScreen);
            m_encoders.Add(MessageType.BalloonContentUpdate, SerializeBalloonContentUpdate);
            m_encoders.Add(MessageType.BalloonDecorationUpdate, SerializeBalloonDecorationUpdate);
            m_encoders.Add(MessageType.PopBalloon, SerializeBalloon);
            m_encoders.Add(MessageType.GetBalloonContent, SerializeBalloon);
            m_encoders.Add(MessageType.GetBalloonDecoration, SerializeBalloon);

            if(Configuration.LogNetworkMessages)
            {
                m_logFormatter = new TextMessageSerializer();
            }
        }

        private void LogMessage(Message msg, string direction)
        {
            if((m_logFormatter != null) && (msg != null))
            {
                try
                {
                    string line = m_logFormatter.Format(msg);
                    Trace.WriteLine(String.Format("{0} {1}", direction, line));
                }
                catch(Exception)
                {
                }
            }
        }

        #region Serialization
        public byte[] Serialize(Message msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException("msg");
            }
            MessageEncoder encoder;
            if (!m_encoders.TryGetValue(msg.Type, out encoder))
            {
                throw new NotImplementedException("Message type not supported: " + msg.Type);
            }
            MemoryStream s = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(s);
            // write placeholder for message size
            writer.Write((int)0);
            // write message type
            writer.Write((int)msg.Type);
            // write message body
            encoder(writer, msg);
            writer.Flush();
            // write message size
            writer.Seek(0, SeekOrigin.Begin);
            writer.Write((int)s.Length);
            writer.Flush();
            LogMessage(msg, ">>");
            return s.ToArray();
        }

        private void SerializeNewBalloon(BinaryWriter writer, Message msg)
        {
            NewBalloonMessage nbm = (NewBalloonMessage)msg;
            writer.Write(nbm.BalloonID);
            writer.Write((int)nbm.Direction);
            writer.Write(nbm.Y);
            writer.Write(nbm.Velocity.X);
            writer.Write(nbm.Velocity.Y);
        }

        private void SerializeChangeScreen(BinaryWriter writer, Message msg)
        {
            ChangeScreenMessage csm = (ChangeScreenMessage)msg;
            writer.Write(csm.BalloonID);
            writer.Write((int)csm.Direction);
            writer.Write(csm.Y);
            writer.Write(csm.Velocity.X);
            writer.Write(csm.Velocity.Y);
        }

        private void SerializeBalloonDecorationUpdate(BinaryWriter writer, Message msg)
        {
            BalloonDecorationUpdateMessage bdm = (BalloonDecorationUpdateMessage)msg;
            writer.Write(bdm.BalloonID);
            writer.Write((int)bdm.OverlayType);
            writer.Write(bdm.BackgroundColor.Red);
            writer.Write(bdm.BackgroundColor.Green);
            writer.Write(bdm.BackgroundColor.Blue);
            writer.Write(bdm.BackgroundColor.Alpha);
        }
        
        private void SerializeBalloonContentUpdate(BinaryWriter writer, Message msg)
        {
            BalloonContentUpdateMessage bcm = (BalloonContentUpdateMessage)msg;
            writer.Write(bcm.BalloonID);
            writer.Write((int)bcm.BalloonType);
            writer.Write(bcm.Label == null ? "" : bcm.Label);
            writer.Write(bcm.Content == null ? "" : bcm.Content);
            writer.Write(bcm.Url == null ? "" : bcm.Url);
            writer.Write(bcm.ImageUrl == null ? "" : bcm.ImageUrl);
        }
        
        private void SerializeBalloon(BinaryWriter writer, Message msg)
        {
            BalloonMessage bm = (BalloonMessage)msg;
            writer.Write(bm.BalloonID);
        }
        #endregion

        #region Deserialization
        public Message Deserialize(CircularBuffer buffer)
        {
            // Read the message size
            if (buffer.Available < 4)
            {
                return null;
            }
            int offset = 0;
            uint size = (uint)((buffer.PeekByte(offset++) << 0) |
                               (buffer.PeekByte(offset++) << 8) |
                               (buffer.PeekByte(offset++) << 16) |
                               (buffer.PeekByte(offset++) << 24));
            if (buffer.Available < (int)size)
            {
                return null;
            }

            // read the message's contents
            byte[] data = new byte[size];
            buffer.Read(data, 0, (int)size);

            // decode the message's contents
            MemoryStream ms = new MemoryStream(data, false);
            BinaryReader reader = new BinaryReader(ms);
            reader.ReadInt32();
            MessageType type = (MessageType)reader.ReadInt32();
            MessageDecoder decoder;
            if (!m_decoders.TryGetValue(type, out decoder))
            {
                throw new NotImplementedException("Message type not supported: " + type);
            }
            Message msg = decoder(reader, type);
            LogMessage(msg, "<<");
            return msg;
        }

        private Message DecodeNewBalloon(BinaryReader reader, MessageType type)
        {
            string balloonID = reader.ReadString();
            Direction direction = (Direction)reader.ReadInt32();
            float y = reader.ReadSingle();
            float velocityX = reader.ReadSingle();
            float velocityY = reader.ReadSingle();
            return new NewBalloonMessage(balloonID, direction, y, new Vector2D(velocityX, velocityY));
        }

        private Message DecodeChangeScreen(BinaryReader reader, MessageType type)
        {
            string balloonID = reader.ReadString();
            Direction direction = (Direction)reader.ReadInt32();
            float y = reader.ReadSingle();
            float velocityX = reader.ReadSingle();
            float velocityY = reader.ReadSingle();
            return new ChangeScreenMessage(balloonID, direction, y, new Vector2D(velocityX, velocityY));
        }

        private Message DecodeBalloonDecorationUpdate(BinaryReader reader, MessageType type)
        {
            string balloonID = reader.ReadString();
            OverlayType overlayType = (OverlayType)reader.ReadInt32();
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            byte a = reader.ReadByte();
            Colour c = new Colour(r, g, b, a);
            return new BalloonDecorationUpdateMessage(balloonID, overlayType, c);
        }
        
        private Message DecodeBalloonContentUpdate(BinaryReader reader, MessageType type)
        {
            string balloonID = reader.ReadString();
            BalloonType balloonType = (BalloonType)reader.ReadInt32();
            string label = reader.ReadString();
            string content = reader.ReadString();
            string url = reader.ReadString();
            string imageUrl = reader.ReadString();
            return new BalloonContentUpdateMessage(balloonID, balloonType, label, content, url, imageUrl);
        }
        
        private Message DecodeBalloon(BinaryReader reader, MessageType type)
        {
            string balloonID = reader.ReadString();
            switch(type)
            {
            case MessageType.PopBalloon:
                return new PopBalloonMessage(balloonID);
            case MessageType.GetBalloonContent:
                return new GetBalloonContentMessage(balloonID);
            case MessageType.GetBalloonDecoration:
                return new GetBalloonDecorationMessage(balloonID);
            default:
                throw new ArgumentOutOfRangeException("type");
            }
        }
        #endregion
    }
}
