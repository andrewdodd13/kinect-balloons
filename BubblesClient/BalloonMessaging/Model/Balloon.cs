using System;

namespace Balloons.Messaging.Model
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
        private string m_id;
        public string Label;
        public string Content;
        public string Url;

        public int Type;
        public Colour BackgroundColor;

        public string ID
        {
            get { return this.m_id; }
        }

        public Balloon(string id)
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

