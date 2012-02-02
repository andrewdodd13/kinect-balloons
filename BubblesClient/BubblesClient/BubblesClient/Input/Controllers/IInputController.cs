using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace BubblesClient.Input.Controllers
{
    /// <summary>
    /// IInputController defines the possible actions that can be performed in
    /// the game. Each Input Method retains a reference to this class, and 
    /// should call the appropriate method when it occurs.
    /// </summary>
    public interface IInputController
    {
        void Initialize(Vector2 screenSize);
        Vector3[] GetHandPositions();
    }
}
