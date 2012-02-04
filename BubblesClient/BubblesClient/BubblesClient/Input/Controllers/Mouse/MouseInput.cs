using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using XnaMouse = Microsoft.Xna.Framework.Input.Mouse;

namespace BubblesClient.Input.Controllers.Mouse
{
    /// <summary>
    /// Mouse Input is an input controller which emulates one hand of the user.
    /// </summary>
    public class MouseInput : IInputController
    {
        private Hand _hand = new Hand();

        /// <summary>
        /// Does nothing; Mouse input is handled by XNA for us
        /// </summary>
        /// <param name="screenSize">The dimensions of the screen (unused)</param>
        public void Initialize(Vector2 screenSize) { }

        /// <summary>
        /// Returns one hand position with the position of the mouse
        /// </summary>
        /// <returns></returns>
        public Hand[] GetHandPositions()
        {
            MouseState ms = XnaMouse.GetState();

            _hand.Position = new Vector3(ms.X, ms.Y, 0);

            return new Hand[] { _hand };
        }
    }
}
