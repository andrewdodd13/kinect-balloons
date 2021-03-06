﻿namespace BubblesClient.Model.ContentBox
{
    using Balloons.Messaging.Model;
    using BubblesClient.Utility;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using System.Collections.Generic;

    /// <summary>
    /// HTML Content Box renders its contents using an HTML rendering library.
    /// </summary>
    public class HTMLContentBox : AbstractContentBox
    {
        // Graphics
        public HtmlRenderer HtmlRenderer { get; private set; }

        private Dictionary<PlaneType, System.Drawing.Bitmap> planeCaptions;

        public HTMLContentBox(Vector2 screenDimensions, GraphicsDeviceManager graphics) :
            base(screenDimensions, graphics)
        {
            this.HtmlRenderer = new HtmlRenderer();
            planeCaptions = new Dictionary<PlaneType, System.Drawing.Bitmap>();
        }

        public override void LoadResources(ContentManager contentManager)
        {
            base.LoadResources(contentManager);

            this.HtmlRenderer.LoadTemplate(contentManager.RootDirectory, "caption_box.html");
            this.HtmlRenderer.LoadTemplate(contentManager.RootDirectory, "content_box.html");
            this.HtmlRenderer.LoadImage(contentManager.RootDirectory, "thumbs-up.png");
            this.HtmlRenderer.LoadImage(contentManager.RootDirectory, "thumbs-down.png");
        }

        /// <summary>
        /// Call this if and only if the visibleBalloon property is not null.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (visibleBalloon == null)
            {
                return;
            }

            Texture2D contentBoxTexture = visibleBalloon.BalloonContentCache[CacheType.Content, graphicsManager.GraphicsDevice];
            if (contentBoxTexture != null)
            {
                // Position contains the co-ordinate of the top-left corner of the box
                Vector2 position = (screenDimensions / 2) -
                    (new Vector2(contentBoxTexture.Width, contentBoxTexture.Height) / 2);

                // Draw the HTML-rendered box
                spriteBatch.Draw(contentBoxTexture, position, Color.White);
            }

            DrawCloseTimer(spriteBatch);
        }

        public override void GenerateCaption(ClientBalloon balloon)
        {
            BalloonContentCache cacheEntry = balloon.BalloonContentCache;
            cacheEntry[CacheType.Caption] = HtmlRenderer.RenderCaption(balloon.Label);
        }

        public override void GenerateTextContent(ClientBalloon balloon)
        {
            BalloonContentCache cacheEntry = balloon.BalloonContentCache;
            cacheEntry[CacheType.Content] = HtmlRenderer.RenderContent(balloon);
        }

        public override void GeneratePlaneCaption(ClientPlane plane)
        {
            System.Drawing.Bitmap img;
            if (!planeCaptions.TryGetValue(plane.Type, out img))
            {
                string template = "<p class=\"helpTitle\">Did you know?</p><p>@@MSG@@</p>";
                string message = template.Replace("@@MSG@@", plane.Message);
                img = HtmlRenderer.RenderCaption(message);
                planeCaptions[plane.Type] = img;
            }
            plane.Caption = img;
        }

        protected override void Close()
        {
            // clear the content box image so that it will be updated next time the balloon is popped
            visibleBalloon.BalloonContentCache[CacheType.Content] = null;
            base.Close();
        }
    }
}
