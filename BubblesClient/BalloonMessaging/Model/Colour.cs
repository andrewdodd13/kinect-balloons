using System;
using System.Globalization;
using System.Text;

namespace Balloons.Messaging.Model
{
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

        public static Colour Parse(string text)
        {
            byte r = 255, g = 255, b = 255, a = 255;
            if((text != null) && text.Length >= 6)
            {
                r = byte.Parse(text.Substring(0, 2), NumberStyles.HexNumber);
                g = byte.Parse(text.Substring(2, 2), NumberStyles.HexNumber);
                b = byte.Parse(text.Substring(4, 2), NumberStyles.HexNumber);
                if(text.Length >= 8)
                {
                    a = byte.Parse(text.Substring(6, 2), NumberStyles.HexNumber);
                }
            }
            return new Colour(r, g, b, a);
        }
    }
}
