using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BubblesClient.Input.Controllers.Mouse
{
    /// <summary>
    /// Mouse Input is an input controller which emulates one hand of the user.
    /// </summary>
    public class MouseInput : IInputController
    {
        /// <summary>
        /// Does nothing; Mouse input is handled by XNA for us
        /// </summary>
        /// <param name="screenSize">The dimensions of the screen (unused)</param>
        public void Initialize(Vector2 screenSize) { }

        /// <summary>
        /// Returns one hand position with the position of the mouse
        /// </summary>
        /// <returns></returns>
        public Vector3[] GetHandPositions()
        {
            MouseState ms = Microsoft.Xna.Framework.Input.Mouse.GetState();

            return new Vector3[] { new Vector3(ms.X, ms.Y, 0) };
        }
    }
}
