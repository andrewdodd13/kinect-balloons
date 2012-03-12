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
        Customizable = 0,
        CustomContent = 1,
        Twitter = 3,
    }

    public enum OverlayType
    {
        White = 0,
        Spots = 1,
        Stripes = 2
    }

    public class Balloon
    {
        public string ID { get; private set; }

        public string Label { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }

        public OverlayType OverlayType { get; set; }
        public BalloonType Type { get; set; }
        public Colour BackgroundColor { get; set; }
        public int Votes { get; set; }

        public Balloon(string id)
        {
            this.ID = id;

            BackgroundColor = new Colour(255, 255, 255, 255);
            Votes = 0;
        }

        public Balloon(Balloon parent) : this(parent.ID)
        {
            // Copy properties from the parent
            this.BackgroundColor = parent.BackgroundColor;
            this.Content = parent.Content;
            this.Label = parent.Label;
            this.OverlayType = parent.OverlayType;
            this.Type = parent.Type;
            this.Url = parent.Url;
            this.Votes = parent.Votes;
            this.ImageUrl = parent.ImageUrl;
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
                case "twitter":
                    return BalloonType.Twitter;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

