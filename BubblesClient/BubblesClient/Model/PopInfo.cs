using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Balloons.Messaging.Model;

namespace BubblesClient.Model
{
    public class PopAnim
    {
        public string ID { get; set; }
        public Vector2 Pos { get; set; }
        public TimeSpan TimePopped { get; set; }
        public float ElapsedSincePopped { get; set; }
        public Texture2D PopTexture { get; set; }
        public Colour PopColour { get; set; }

        public PopAnim(ClientBalloon balloon)
        {
            this.ID = balloon.ID;
        }
    }
}
