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
        private Hand _altHand = new Hand();
        private float Seperation = 0, MaxSeperation = 200;
        private bool anim = false;

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

            if (ms.RightButton == ButtonState.Pressed)
            {
                if (ms.LeftButton == ButtonState.Pressed)
                {
                    anim = true;
                }
                if (anim)
                {
                    Seperation = Seperation - 20;
                }
                _hand.Position = new Vector3(ms.X - Seperation, ms.Y, 0);
                _altHand.Position = new Vector3(ms.X + Seperation, ms.Y, 0);
                if (Seperation <= 0)
                {
                    Seperation = MaxSeperation;
                    anim = false;
                }
                return new Hand[] { _hand, _altHand };
            }
            else
            {
                _hand.Position = new Vector3(ms.X, ms.Y, 0);
                return new Hand[] { _hand };
            }
        }

        /// <summary>
        /// Returns true if the middle mouse button is clicked.
        /// </summary>
        /// <returns></returns>
        public bool ShouldClosePopup()
        {
            return (XnaMouse.GetState().MiddleButton == ButtonState.Pressed);
        }
    }
}
