using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BubblesClient.Model;
using Microsoft.Xna.Framework;
using BubblesClient.Network.Event;

namespace BubblesClient.Network
{
    /// <summary>
    /// INetworkEventManager is the interface which must be implemented by the
    /// Class which deals with pulling messages out of the network 
    /// communications layer and presents them to the game.
    /// The main game engine will poll this class on every frame to retrieve
    /// changes to the server state, and then to notify the server of any 
    /// changes to state it needs to make.
    /// </summary>
    public interface INetworkEventManager
    {
        /// <summary>
        /// Returns a list of new Balloons Events added by the server since the
        /// last call to this function.
        /// </summary>
        /// <returns></returns>
        List<NewBalloonEvent> GetNewBalloons();

        /// <summary>
        /// Returns a list of Balloons which the server has asked us to pop 
        /// since the last call to this function. 
        /// </summary>
        /// <returns></returns>
        List<Balloon> GetPoppedBalloons();

        /// <summary>
        /// Called by the game loop to notify the server that a user has popped
        /// a balloon on this screen.
        /// </summary>
        /// <param name="balloon">The Balloon to pop</param>
        void NotifyBalloonPop(Balloon balloon);

        /// <summary>
        /// Called by the game loop to notify the server that a balloon has 
        /// exited the screen.
        /// </summary>
        /// <param name="balloon">The Balloon which left the screen</param>
        /// <param name="position">The position of the balloon when it left the
        /// screen. The server will take care of deciding where the balloon 
        /// should go based on the parameter so it must be correct.</param>
        /// <param name="velocity">The velocity at which the balloon left the 
        /// screen.</param>
        void NotifyBalloonExit(Balloon balloon, Vector2 position, Vector2 velocity);

        /// <summary>
        /// Called by the game loop to notify the server that a user has made
        /// a change to a balloon. Generally this should be done when the user
        /// is finished making changes to the decorations.
        /// </summary>
        /// <param name="balloon">The balloon object to notify the server 
        /// about.</param>
        void NotifyBalloonUpdated(Balloon balloon);
    }
}
