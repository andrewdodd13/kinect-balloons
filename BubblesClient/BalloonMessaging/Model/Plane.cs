using System;

namespace Balloons.Messaging.Model
{
    public enum PlaneType
    {
        BurstBallons,
        PushBallons
    };

    public class Plane
    {
        public string ID { get; private set; }
        public float Time { get; private set; }
        public PlaneType Type { get; private set; }

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
