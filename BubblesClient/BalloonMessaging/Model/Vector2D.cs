namespace Balloons.Messaging.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Vector2D
    {
        public float X { get; private set; }
        public float Y { get; private set; }

        public Vector2D(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
