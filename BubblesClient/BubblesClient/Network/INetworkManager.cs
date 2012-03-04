namespace BubblesClient.Network
{
    using System;
    using BubblesClient.Model;
    using Balloons.Messaging.Model;
    using Microsoft.Xna.Framework;
    using System.Collections.Generic;

    /// <summary>
    /// INetworkManager is the interface for a component which communicates 
    /// with the Server and provides methods for notifying the server of state
    /// changes.
    /// </summary>
    public interface INetworkManager : IDisposable
    {
        /// <summary>
        /// Connects the Network Manager to the Server.
        /// </summary>
        void Connect();

        /// <summary>
        /// Notifies the Server that a balloon has moved off screen. The 
        /// implementation is responsible for ensuring that this event is not 
        /// called twice for the same balloon.
        /// </summary>
        /// <param name="balloon">The balloon to move</param>
        /// <param name="direction">The side of the screen the balloon is 
        /// exiting via</param>
        /// <param name="exitHeight">The normalised position on the screen the 
        /// balloon was at when it left the screen</param>
        /// <param name="velocity">The velocity of the balloon when it left the
        /// screen</param>
        void MoveBalloonOffscreen(ClientBalloon balloon, Direction direction, float exitHeight, Vector2 velocity);

        /// <summary>
        /// Notifies the Server that a balloon has been popped by a user.
        /// </summary>
        /// <param name="balloon"></param>
        void NotifyBalloonPopped(ClientBalloon balloon);

        /// <summary>
        /// Retrieves the details of a balloon from the Server.
        /// </summary>
        /// <param name="balloonID"></param>
        /// <returns></returns>
        Balloon GetBalloonDetails(string balloonID);

        /// <summary>
        /// Notifies the Server that a balloon's details have changed 
        /// (usually its decoration).
        /// </summary>
        /// <param name="balloon"></param>
        void UpdateBalloonDetails(Balloon balloon);

        /// <summary>
        /// Retrieves all the messages that the Network Manager has received
        /// from the Server since the last call to this function.
        /// </summary>
        /// <returns></returns>
        List<Message> GetAllMessages();
    }
}
