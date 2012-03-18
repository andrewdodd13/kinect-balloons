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

        /// <summary>
        /// Time the plane has left to live. Decreases every time the plane leaves a screen.
        /// </summary>
        public int Ttl
        {
            get;
            set;
        }

        public ServerPlane(string planeID, PlaneType type)
            : base(planeID, type)
        {
        }
    }
}
