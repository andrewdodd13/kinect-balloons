using Balloons.Messaging.Model;
using Microsoft.Xna.Framework.Graphics;

namespace BubblesClient.Model
{
    /// <summary>
    /// ClientBalloon is an extension of Balloon which contains details 
    /// specific to displaying the balloon on the client side.
    /// </summary>
    public class ClientBalloon : Balloon
    {
        /// <summary>
        /// This is true if the balloon's position is outside of the screen and
        /// the server has been notified of it.
        /// </summary>
        public bool OffScreen;

        /// <summary>
        /// Whether or not the label has been cached.
        /// This is used for wrapping the text of the label
        /// When the wrapping has been doen once this will be set to true 
        /// so that it does not happen again every time the screen is drawn
        /// </summary>
        public bool IsLabelCached { get; set; }

        public const float BalloonWidth = 162f;
        public const float BalloonHeight = 192f;

        public Texture2D Texture { get; set; }
        public BalloonContentCache BalloonContentCache { get; set; }

        public bool Popped { get; set; }

        public ClientBalloon(Balloon parent)
            : base(parent)
        {
            this.Popped = false;
            this.IsLabelCached = false;
        }
    }
}
