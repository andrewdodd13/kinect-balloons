using System;
using Balloons.Messaging.Model;

namespace Balloons.Server
{
    public class ServerBalloon : Balloon
    {
        private Screen m_screen;

        public Screen Screen
        {
            get { return this.m_screen; }
            set { m_screen = value; }
        }

        public ServerBalloon(string balloonID)
            : base(balloonID)
        {
        }
    }
}
