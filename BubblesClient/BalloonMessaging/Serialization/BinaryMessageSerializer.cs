using System;
using System.Collections.Generic;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Serialization
{
    public class BinaryMessageSerializer : IMessageSerializer
    {
        public byte[] Serialize(Message msg)
        {
            throw new NotImplementedException();
        }

        public Message Deserialize(CircularBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
