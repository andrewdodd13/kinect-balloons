using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BubblesClient.Input.Controllers
{
    /// <summary>
    /// IBubblesInputMethod is the interface representing an Input Method (IM).
    /// When the system creates an IM it calls the Initialise method with a 
    /// reference to an IInputController (which should be stored by any 
    /// implementations). Then, for each Frame of the game, the Frame method is
    /// called. The IM should process any input and if necessary, call the 
    /// appropriate method on the controller.
    /// </summary>
    interface IBubblesInputMethod
    {
        void Initialise(IInputController controller);
        void Frame();
    }
}
