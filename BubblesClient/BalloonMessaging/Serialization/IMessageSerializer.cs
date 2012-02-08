using System;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Serialization
{
    public interface IMessageSerializer
    {
        /// <summary>
        /// Convert a message to an array of bytes.
        /// </summary>
        /// <param name="msg"> Message to serialize. </param>
        byte[] Serialize(Message msg);

        /// <summary>
        /// Try to read a message from the current buffered data.
        /// </summary>
        /// <returns>
        /// Message read or null if there is not enough data for a complete message.
        /// </returns>
         Message Deserialize(CircularBuffer buffer);
    }
}
