using System;
using Balloons.Messaging.Model;

namespace Balloons.Server
{
    class ServerPlane : Plane
    {
        private Screen m_screen;

        public Screen Screen
        {
            get { return this.m_screen; }
            set { m_screen = value; }
        }

        public ServerPlane(string planeID, PlaneType type)
            : base(planeID, type)
        {
        }
    }
}
