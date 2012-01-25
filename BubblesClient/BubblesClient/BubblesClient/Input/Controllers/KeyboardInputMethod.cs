using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace BubblesClient.Input.Controllers
{
    /// <summary>
    /// KeyboardInputMethod is an IM which takes its input from the keyboard.
    /// On each Frame, it checks the current keyboard state for any keys being
    /// down. If this key is down (and it was not down in the previous Frame),
    /// then the appropriate event is fired.
    /// </summary>
    class KeyboardInputMethod : IBubblesInputMethod
    {
        private IInputController _inputController;
        private KeyboardState _oldState;

        public void Initialise(IInputController inputController)
        {
            _inputController = inputController;
        }

        public void Frame()
        {
            KeyboardState newState = Keyboard.GetState();

            // If we press L, then emulate a swipe left command.
            // Obviously this was completely chosen at random...
            if (newState.IsKeyDown(Keys.L))
            {
                if (!_oldState.IsKeyDown(Keys.L))
                {
                    _inputController.SwipeLeft();
                }
            }

            _oldState = newState;
        }
    }
}
