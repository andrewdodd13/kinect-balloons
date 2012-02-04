using System;
using System.Drawing;
using Balloons;

namespace Balloons.Server
{
    public class ServerBalloon : Balloon
    {
        public static readonly PointF VelocityLeft = new PointF(-0.1f, 0.0f);
        public static readonly PointF VelocityRight = new PointF(0.1f, 0.0f);

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
