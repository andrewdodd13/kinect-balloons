using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DummyClient
{
    public class Balloon
    {
        public int ID;
        public Vector2 Pos;
        public Vector2 Velocity;
        /// <summary>
        /// This is true if the balloon's position is outside of the screen and
        /// the server has been notified of it.
        /// </summary>
        public bool OffScreen;
        
        public string Label;
        public string Content;
        public string Url;

        public int Type;
        public int OverlayType;
        public Vector3 BackgroundColor;
    }
}
