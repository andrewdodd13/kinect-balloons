namespace Balloons.Messaging.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Colour class for use by Balloons! Mainly because the yanks cannot spell.
    /// </summary>
    public class Colour
    {
        public byte Alpha { get; private set; }
        public byte Blue { get; private set; }
        public byte Green { get; private set; }
        public byte Red { get; private set; }

        public Colour(byte red, byte green, byte blue, byte alpha)
        {
            this.Red = red;
            this.Blue = blue;
            this.Green = green;
            this.Alpha = alpha;
        }
    }
}
