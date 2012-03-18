using System;

namespace Balloons.Messaging.Model
{
    public enum PlaneType
    {
        BurstBallons,
        PushBallons,
        InvalidType
    };

    public class Plane
    {
        public string ID { get; private set; }
        public float Time { get; set; }
        public PlaneType Type { get; set; }

        public string Message
        {
            get
            {
                switch (Type)
                {
                case PlaneType.BurstBallons:
                    return "You can burst balloons by clapping your hands together.";
                case PlaneType.PushBallons:
                    return "You can customise balloons by pushing them on top of the paint buckets.";
                default:
                    return "&lt;Unknown message&gt;"; 
                }
            }
        }

        public Plane(string id, PlaneType type, float time = 0)
        {
            this.ID = id;
            this.Type = type;
            this.Time = time;
        }

        public Plane(Plane parent)
            : this(parent.ID, parent.Type, parent.Time)
        {
        }
    }
}
