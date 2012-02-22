namespace BubblesClient.Model
{
    using System;
    using Microsoft.Xna.Framework.Graphics;

    public class BalloonContentCache
    {
        public string ID { get; set; }
        public Texture2D QRCode { get; set; }
        public Texture2D Image { get; set; }
        public Texture2D Caption { get; set; }

        public BalloonContentCache(string id)
        {
            this.ID = id;
        }
    }
}
