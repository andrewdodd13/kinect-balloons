using System;
using Balloons.Messaging.Model;

namespace Balloons.Server
{
    public class ServerBalloon : Balloon
    {
        public static readonly Vector2D VelocityLeft = new Vector2D(-0.1f, 0.0f);
        public static readonly Vector2D VelocityRight = new Vector2D(0.1f, 0.0f);

        private Screen m_screen;

        public Screen Screen
        {
            get { return this.m_screen; }
            set { m_screen = value; }
        }

        public ServerBalloon(int balloonID)
            : base(balloonID)
        {
        }
    }
}
