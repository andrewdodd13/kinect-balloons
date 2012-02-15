using System;
using Balloons.Messaging.Model;
using Microsoft.Xna.Framework.Graphics;
using ThoughtWorks.QRCode.Codec;
using System.IO;

namespace BubblesClient.Model
{
    public class ClientBalloon : Balloon
    {
        /// <summary>
        /// This is true if the balloon's position is outside of the screen and
        /// the server has been notified of it.
        /// </summary>
        public bool OffScreen;

        public const float BalloonWidth = 162f;
        public const float BalloonHeight = 192f;

        public Texture2D Texture { get; set; }
        public Texture2D QrCodeTexture { get; set; }

        public bool Popped { get; set; }

        public ClientBalloon(Balloon parent)
            : base(parent)
        {
            Popped = false;
        }
    }
}
