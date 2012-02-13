using Balloons.Messaging.Model;
using FarseerPhysics.Dynamics;

namespace Balloons.DummyClient
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
            : base(parent.ID)
        {
            this.Body = body;

            // Copy properties from the parent
            this.BackgroundColor = parent.BackgroundColor;
            this.Content = parent.Content;
            this.Label = parent.Label;
            this.OverlayType = parent.OverlayType;
            this.Type = parent.Type;
            this.Url = parent.Url;
        }
    }
}
