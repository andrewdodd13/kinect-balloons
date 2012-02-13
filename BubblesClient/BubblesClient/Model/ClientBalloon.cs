using Balloons.Messaging.Model;
using FarseerPhysics.Dynamics;

namespace BubblesClient.Model
{
    public class ClientBalloon : Balloon
    {
        /// <summary>
        /// This is true if the balloon's position is outside of the screen and
        /// the server has been notified of it.
        /// </summary>
        public bool OffScreen;

        public Body Body { get; private set; }

        public ClientBalloon(Balloon parent, Body body)
            : base(parent)
        {
            this.Body = body;
        }
    }
}
