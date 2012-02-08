using System;
using System.Collections.Generic;
using System.IO;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Serialization
{
    public class BinaryMessageSerializer : IMessageSerializer
    {
        private delegate Message MessageDecoder(BinaryReader reader);
        private delegate void MessageEncoder(BinaryWriter writer, Message msg);
        
        private Dictionary<MessageType, MessageDecoder> m_decoders;
        private Dictionary<MessageType, MessageEncoder> m_encoders;
        
        public BinaryMessageSerializer()
        {
            m_decoders = new Dictionary<MessageType, MessageDecoder>();
            m_decoders.Add(MessageType.NewBalloon, DecodeNewBalloon);
            m_decoders.Add(MessageType.ChangeScreen, DecodeChangeScreen);
            m_decoders.Add(MessageType.PopBalloon, DecodePopBalloon);
            
            m_encoders = new Dictionary<MessageType, MessageEncoder>();
            m_encoders.Add(MessageType.NewBalloon, SerializeNewBalloon);
            m_encoders.Add(MessageType.ChangeScreen, SerializeChangeScreen);
            m_encoders.Add(MessageType.PopBalloon, SerializePopBalloon);
        }
        
        #region Serialization
        public byte[] Serialize(Message msg)
        {
            if(msg == null)
            {
                throw new ArgumentNullException("msg");
            }
            MessageEncoder encoder;
            if(!m_encoders.TryGetValue(msg.Type, out encoder))
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
        
        private void SerializePopBalloon(BinaryWriter writer, Message msg)
        {
            PopBalloonMessage pbm = (PopBalloonMessage)msg;
            writer.Write(pbm.BalloonID);
        }
        #endregion
        
        #region Deserialization
        public Message Deserialize(CircularBuffer buffer)
        {
            // Read the message size
            if(buffer.Available < 4)
            {
                return null;
            }
            int offset = 0;
            uint size = (uint)((buffer.PeekByte(offset++) << 0)  |
                               (buffer.PeekByte(offset++) << 8)  |
                               (buffer.PeekByte(offset++) << 16) |
                               (buffer.PeekByte(offset++) << 24));
            if(buffer.Available < (int)size)
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
            if(!m_decoders.TryGetValue(type, out decoder))
            {
                throw new NotImplementedException("Message type not supported: " + type);
            }
            return decoder(reader);
        }
        
        private Message DecodeNewBalloon(BinaryReader reader)
        {
            int balloonID = reader.ReadInt32();
            Direction direction = (Direction)reader.ReadInt32();
            float y = reader.ReadSingle();
            float velocityX = reader.ReadSingle();
            float velocityY = reader.ReadSingle();
            return new NewBalloonMessage(balloonID, direction, y, new Vector2D(velocityX, velocityY));
        }
        
        private Message DecodeChangeScreen(BinaryReader reader)
        {
            int balloonID = reader.ReadInt32();
            Direction direction = (Direction)reader.ReadInt32();
            float y = reader.ReadSingle();
            float velocityX = reader.ReadSingle();
            float velocityY = reader.ReadSingle();
            return new ChangeScreenMessage(balloonID, direction, y, new Vector2D(velocityX, velocityY));
        }
        
        private Message DecodePopBalloon(BinaryReader reader)
        {
            int balloonID = reader.ReadInt32();
            return new PopBalloonMessage(balloonID);
        }
        #endregion
    }
}
