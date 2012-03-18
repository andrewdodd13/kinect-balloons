namespace Balloons.Messaging.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Vector2D
    {
        public static readonly Vector2D Zero = new Vector2D(0.0f, 0.0f);

        public float X { get; private set; }
        public float Y { get; private set; }

        public Vector2D(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
