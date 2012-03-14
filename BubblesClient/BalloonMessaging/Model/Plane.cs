﻿using System;

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
        public float Time { get; private set; }
        public PlaneType Type { get; private set; }
        public string Message
        {
            get
            {
                switch (Type)
                {
                case PlaneType.BurstBallons:
                    return "<h3>Did you know?</h3><p>You can burst balloons by clapping your hands together.</p>";
                case PlaneType.PushBallons:
                    return "<h3>Did you know?</h3><p>You can customise balloons by pushing them on top of the paint buckets.</p>";
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
