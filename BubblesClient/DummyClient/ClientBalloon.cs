using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Balloons;

namespace Balloons.DummyClient
{
    public class ClientBalloon : Balloon
    {
        public Vector2 Pos;
        public Vector2 Velocity;
        /// <summary>
        /// This is true if the balloon's position is outside of the screen and
        /// the server has been notified of it.
        /// </summary>
        public bool OffScreen;

        public ClientBalloon(int balloonID)
            : base(balloonID)
        {
        }
    }
}
