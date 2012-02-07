using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace BubblesClient.Input.Controllers
{
    /// <summary>
    /// Hand is a mutable identifier for a user's hand. It's used by the system
    /// to keep state of hands between frames.
    /// </summary>
    public class Hand
    {
        public Vector3 Position { get; set; }
    }
}
