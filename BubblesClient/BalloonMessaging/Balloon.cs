using System;
using System.Drawing;

namespace Balloons
{
    /// <summary>
    /// Used to describe the direction taken by a bubble when it leaves a screen.
    /// </summary>
    public enum Direction
    {
        Any,
        Left,
        Right
    }

    public class Balloon
    {
        private int m_id;
        public string Label;
        public string Content;
        public string Url;

        public int Type;
        public int OverlayType;
        public Color BackgroundColor;

        public int ID
        {
            get { return this.m_id; }
        }

        public Balloon(int id)
        {
            m_id = id;
        }

        public static string FormatDirection(Direction direction)
        {
            switch(direction)
            {
            case Direction.Left:
                return "left";
            case Direction.Right:
                return "right";
            case Direction.Any:
                return "any";
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public static Direction ParseDirection(string text)
        {
            switch(text)
            {
            case "left":
                return Direction.Left;
            case "right":
                return Direction.Right;
            case "any":
                return Direction.Any;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}

