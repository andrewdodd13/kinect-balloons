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
            m_decoders.Add(MessageType.NewPlane, DecodeNewPlane);
            m_decoders.Add(MessageType.ChangeScreen, DecodeChangeScreen);
            m_decoders.Add(MessageType.BalloonContentUpdate, DecodeBalloonContentUpdate);
            m_decoders.Add(MessageType.BalloonStateUpdate, DecodeBalloonStateUpdate);
            m_decoders.Add(MessageType.PopObject, DecodeBalloon);
            m_decoders.Add(MessageType.GetBalloonContent, DecodeBalloon);
            m_decoders.Add(MessageType.GetBalloonState, DecodeBalloon);

            m_encoders = new Dictionary<MessageType, MessageEncoder>();
            m_encoders.Add(MessageType.NewBalloon, SerializeNewBalloon);
            m_encoders.Add(MessageType.NewPlane, SerializeNewPlane);
            m_encoders.Add(MessageType.ChangeScreen, SerializeChangeScreen);
            m_encoders.Add(MessageType.BalloonContentUpdate, SerializeBalloonContentUpdate);
            m_encoders.Add(MessageType.BalloonStateUpdate, SerializeBalloonStateUpdate);
            m_encoders.Add(MessageType.PopObject, SerializeBalloon);
            m_encoders.Add(MessageType.GetBalloonContent, SerializeBalloon);
            m_encoders.Add(MessageType.GetBalloonState, SerializeBalloon);

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
            writer.Write(nbm.ObjectID);
            writer.Write((int)nbm.Direction);
            writer.Write(nbm.Y);
            writer.Write(nbm.Velocity.X);
            writer.Write(nbm.Velocity.Y);
        }

        private void SerializeNewPlane(BinaryWriter writer, Message msg)
        {
            NewPlaneMessage npm = (NewPlaneMessage)msg;
            writer.Write(npm.ObjectID);
            writer.Write((int)npm.PlaneType);
            writer.Write((int)npm.Direction);
            writer.Write(npm.Y);
            writer.Write(npm.Velocity.X);
            writer.Write(npm.Velocity.Y);
            writer.Write(npm.Time);
        }

        private void SerializeChangeScreen(BinaryWriter writer, Message msg)
        {
            ChangeScreenMessage csm = (ChangeScreenMessage)msg;
            writer.Write(csm.ObjectID);
            writer.Write((int)csm.Direction);
            writer.Write(csm.Y);
            writer.Write(csm.Velocity.X);
            writer.Write(csm.Velocity.Y);
        }

        private void SerializeBalloonStateUpdate(BinaryWriter writer, Message msg)
        {
            BalloonStateUpdateMessage bdm = (BalloonStateUpdateMessage)msg;
            writer.Write(bdm.ObjectID);
            writer.Write((int)bdm.OverlayType);
            writer.Write(bdm.BackgroundColor.Red);
            writer.Write(bdm.BackgroundColor.Green);
            writer.Write(bdm.BackgroundColor.Blue);
            writer.Write(bdm.BackgroundColor.Alpha);
            writer.Write(bdm.Votes);
        }
        
        private void SerializeBalloonContentUpdate(BinaryWriter writer, Message msg)
        {
            BalloonContentUpdateMessage bcm = (BalloonContentUpdateMessage)msg;
            writer.Write(bcm.ObjectID);
            writer.Write((int)bcm.BalloonType);
            writer.Write(bcm.Label == null ? "" : bcm.Label);
            writer.Write(bcm.Content == null ? "" : bcm.Content);
            writer.Write(bcm.Url == null ? "" : bcm.Url);
            writer.Write(bcm.ImageUrl == null ? "" : bcm.ImageUrl);
        }
        
        private void SerializeBalloon(BinaryWriter writer, Message msg)
        {
            ObjectMessage bm = (ObjectMessage)msg;
            writer.Write(bm.ObjectID);
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

        private Message DecodeNewPlane(BinaryReader reader, MessageType type)
        {
            string balloonID = reader.ReadString();
            PlaneType planetype = (PlaneType)reader.ReadInt32();
            Direction direction = (Direction)reader.ReadInt32();
            float y = reader.ReadSingle();
            float velocityX = reader.ReadSingle();
            float velocityY = reader.ReadSingle();
            float time = reader.ReadSingle();
            return new NewPlaneMessage(balloonID, planetype, direction, y, new Vector2D(velocityX, velocityY), time);
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

        private Message DecodeBalloonStateUpdate(BinaryReader reader, MessageType type)
        {
            string balloonID = reader.ReadString();
            OverlayType overlayType = (OverlayType)reader.ReadInt32();
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            byte a = reader.ReadByte();
            Colour c = new Colour(r, g, b, a);
            int votes = reader.ReadInt32();
            return new BalloonStateUpdateMessage(balloonID, overlayType, c, votes);
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
            case MessageType.PopObject:
                return new PopObjectMessage(balloonID);
            case MessageType.GetBalloonContent:
                return new GetBalloonContentMessage(balloonID);
            case MessageType.GetBalloonState:
                return new GetBalloonStateMessage(balloonID);
            default:
                throw new ArgumentOutOfRangeException("type");
            }
        }
        #endregion
    }
}
