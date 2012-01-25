using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using BubblesClient.Input.Controllers;

namespace BubblesClient.Input.Manager
{
    /// <summary>
    /// InputManager is the default Input Manager for the Bubbles game.
    /// </summary>
    class InputManager : IInputController, IInputManager
    {
        // The input method to use
        private IBubblesInputMethod _inputMethod;

        public void Initialise(IBubblesInputMethod inputMethod)
        {
            _inputMethod = inputMethod;
            _inputMethod.Initialise(this);
        }

        public void BeginFrame()
        {
            // Call Frame on the input method
            _inputMethod.Frame();
        }

        public void EndFrame()
        {
            // Release any buttons which were depressed during the frame
            _swipedLeft = ButtonState.Released;
        }

        #region "Actions"
        private ButtonState _swipedLeft;
        public ButtonState SwipeLeftControl
        {
            get { return _swipedLeft; }
        }

        public void SwipeLeft()
        {
            _swipedLeft = ButtonState.Pressed;
        }
        #endregion
    }
}
