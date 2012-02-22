namespace BubblesClient.Model
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Microsoft.Xna.Framework.Graphics;
    using BubblesClient.Utility;

    public enum CacheType
    {
        QRCode,
        WebImage,
        Caption,
        Content
    }

    public class BalloonContentCache
    {
        private Dictionary<CacheType, Bitmap> _images;
        private Dictionary<CacheType, Texture2D> _textures;

        public string ID { get; set; }

        public Bitmap this[CacheType type]
        {
            get
            {
                Bitmap img;
                lock (this)
                {
                    _images.TryGetValue(type, out img);
                }
                return img;
            }
            set
            {
                // changing the image invalidates the texture
                lock (this)
                {
                    if (_textures.ContainsKey(type))
                    {
                        _textures.Remove(type);
                    }
                    _images[type] = value;
                }
            }
        }

        public Texture2D this[CacheType type, GraphicsDevice device]
        {
            get
            {
                Texture2D tex = null;
                Bitmap img = null;
                lock (this)
                {
                    if (!_textures.TryGetValue(type, out tex) && _images.TryGetValue(type, out img))
                    {
                        // generate texture from image
                        tex = ImageGenerator.BitmapToTexture(img, device);
                        _textures[type] = tex;
                    }
                }
                return tex;
            }
        }

        public BalloonContentCache(string id)
        {
            this.ID = id;
            this._images = new Dictionary<CacheType, Bitmap>();
            this._textures = new Dictionary<CacheType, Texture2D>();
        }
    }
}
