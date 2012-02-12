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

    public enum BalloonType
    {
        Customizable,
        Twitter,
        News,
        CustomContent
    }

    public class Balloon
    {
        public int ID { get; private set; }

        public string Label { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }

        public BalloonType Type { get; set; }
        public int OverlayType { get; set; }
        public Colour BackgroundColor { get; set; }

        public Balloon(int id)
        {
            this.ID = id;

            BackgroundColor = new Colour(255, 255, 255, 255);
        }

        public static string FormatDirection(Direction direction)
        {
            switch (direction)
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
            switch (text)
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

        public static string FormatBalloonType(BalloonType btype)
        {
            switch (btype)
            {
                case BalloonType.CustomContent:
                    return "customcontent";
                case BalloonType.Customizable:
                    return "customizable";
                case BalloonType.News:
                    return "news";
                case BalloonType.Twitter:
                    return "twitter";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static BalloonType ParseBalloonType(string text)
        {
            switch (text)
            {
                case "customcontent":
                    return BalloonType.CustomContent;
                case "customizable":
                    return BalloonType.Customizable;
                case "news":
                    return BalloonType.News;
                case "twitter":
                    return BalloonType.Twitter;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

