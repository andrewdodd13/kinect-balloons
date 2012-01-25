using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using BubblesClient.Input.Controllers;

namespace BubblesClient.Input.Manager
{
    /// <summary>
    /// IInputManager is the interface implemented by the Input Manager. 
    /// </summary>
    interface IInputManager
    {
        /// <summary>
        /// Initialises the Input Manager.
        /// </summary>
        /// <param name="inputMethod">The Input Method which should be used to
        /// fetch user input.
        /// </param>
        void Initialise(IBubblesInputMethod inputMethod);

        /// <summary>
        /// Called when the frame render is started
        /// </summary>
        void BeginFrame();

        /// <summary>
        /// Called when the frame render is finished
        /// </summary>
        void EndFrame();

        /// <summary>
        /// Inside this region, a series of Properties representing the current
        /// state of the Input Manager should be defined. 
        /// </summary>
        #region "Input Actions"

        ButtonState SwipeLeftControl { get; }

        #endregion
    }
}
