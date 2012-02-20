using System;
using Microsoft.Xna.Framework.Graphics;

namespace BubblesClient.Model
{
    public class PopInfo
    {
        public string ID { get; set; }
        public ClientBalloon Balloon { get; set; }
        public TimeSpan TimePopped { get; set; }
        public Texture2D ContentBox { get; set; }

        public PopInfo(ClientBalloon balloon)
        {
            if(balloon == null)
            {
                throw new ArgumentNullException("balloon");
            }
            this.ID = balloon.ID;
            this.Balloon = balloon;
        }
    }
}
