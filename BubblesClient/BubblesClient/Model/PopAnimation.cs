using System;
using Balloons.Messaging.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BubblesClient.Model
{
    public class PopAnimation
    {
        public string ID { get; set; }
        public Vector2 Pos { get; set; }
        public TimeSpan TimePopped { get; set; }
        public float ElapsedSincePopped { get; set; }
        public Texture2D PopTexture { get; set; }
        public bool PoppedByUser { get; set; }

        public PopAnimation(ClientBalloon balloon)
        {
            this.ID = balloon.ID;
        }
    }
}
