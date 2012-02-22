namespace BubblesClient.Model
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Balloons.Messaging.Model;
    using BubblesClient.Model.ContentBox;
    using BubblesClient.Utility;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;

    public class ManualContentBox : AbstractContentBox
    {
        // Graphics
        private Vector2 screenDimensions;
        private GraphicsDeviceManager graphics;

        // Texture cache
        private Dictionary<string, BalloonContentCache> balloonTextureCache = new Dictionary<string, BalloonContentCache>();

        // Currently wrapped texts
        private string wrappedTitle;
        private string wrappedContent;

        // Timers
        private const float DefaultForceCloseTime = 2000.0f;
        private float forceCloseTimer = DefaultForceCloseTime;
        private float closeTimer = Configuration.MessageDisplayTime;

        public SpriteFont ContentFont { get; set; }
        public SpriteFont TitleFont { get; set; }
        public Texture2D BoxTexture { get; set; }
        public Texture2D CloseIconTexture { get; set; }
        public Texture2D LoadingSprite { get; set; }
        public List<Texture2D> CountDownImages { get; set; }



        public ManualContentBox(Vector2 screenDimensions, GraphicsDeviceManager graphics)
        {
            this.screenDimensions = screenDimensions;
            this.graphics = graphics;
        }

        public override void LoadResources(ContentManager contentManager)
        {
            this.TitleFont = contentManager.Load<SpriteFont>("Fonts/Content-Title");
            this.ContentFont = contentManager.Load<SpriteFont>("Fonts/SpriteFontSmall");
            this.BoxTexture = contentManager.Load<Texture2D>("Images/ContentBox");
            this.CloseIconTexture = contentManager.Load<Texture2D>("Images/CloseIcon");
            this.LoadingSprite = contentManager.Load<Texture2D>("Images/LoadingSprite");

            this.CountDownImages = new List<Texture2D>();
            for (int i = 0; i <= 30; i++)
            {
                this.CountDownImages.Add(contentManager.Load<Texture2D>("Images/Countdown/" + i));
            }
        }

        public override void Update(GameTime gameTime)
        {
            closeTimer -= gameTime.ElapsedGameTime.Milliseconds;
            if (closeTimer < 0) { Close(); }
        }

        /// <summary>
        /// Call this if and only if the visibleBalloon property is not null.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            // Position contains the co-ordinate of the top-left corner of the box excluding border
            Vector2 border = new Vector2(9, 9);
            Vector2 position = (screenDimensions / 2) - (new Vector2(BoxTexture.Width, BoxTexture.Height) / 2) + border;

            // Draw the box itself
            spriteBatch.Draw(BoxTexture, position - border, Color.White);

            // Draw the title 
            TextUtility.drawTextLabel(spriteBatch, TitleFont, wrappedTitle, position + new Vector2(4, 0));

            // Draw the text
            TextUtility.drawTextLabel(spriteBatch, ContentFont, wrappedContent, position + new Vector2(4, 64));

            // Draw the votes
            Color votesColor = Color.Black;
            if (visibleBalloon.Votes > 0) { votesColor = Color.Green; }
            else if (visibleBalloon.Votes < 0) { votesColor = Color.Red; }

            String votesLabel = (visibleBalloon.Votes > 0 ? "+" : "") + visibleBalloon.Votes;
            TextUtility.drawTextLabel(spriteBatch, TitleFont, votesLabel, position + new Vector2(754 - TitleFont.MeasureString(votesLabel).X, 0), votesColor);

            // Draw the QR Code
            if (visibleBalloon.BalloonContentCache.QRCode != null)
            {
                spriteBatch.Draw(visibleBalloon.BalloonContentCache.QRCode, position + new Vector2(BoxTexture.Width - 24 - 224 - 9, BoxTexture.Height - 224 - 24 - 9), Color.White);
            }
            else
            {
                spriteBatch.Draw(LoadingSprite, position + new Vector2(BoxTexture.Width - 24 - 224 - 9, BoxTexture.Height - 224 - 24 - 9), Color.White);
            }

            // Draw the Image
            Texture2D balloonImage = visibleBalloon.BalloonContentCache.Image;
            if (balloonImage != null)
            {
                spriteBatch.Draw(balloonImage, position + new Vector2(BoxTexture.Width - 24 - 112 - (balloonImage.Width / 2) - 9, 24 + 112 - (balloonImage.Height / 2)), Color.White);
            }
            else
            {
                spriteBatch.Draw(LoadingSprite, position + new Vector2(BoxTexture.Width - 24 - LoadingSprite.Width - 9, 24), Color.White);
            }

            // Draw the timer
            Texture2D currentFrame = CountDownImages[(int)(closeTimer / 1000)];
            spriteBatch.Draw(currentFrame, screenDimensions - new Vector2(currentFrame.Width + 8, currentFrame.Height + 8), Color.White);

            // Draw the close icon
            Color closeIconColor = Color.White;
            closeIconColor.A = (byte)((0.25 + (0.75 * ((DefaultForceCloseTime - forceCloseTimer) / DefaultForceCloseTime))) * 255);
            spriteBatch.Draw(CloseIconTexture, new Vector2(screenDimensions.X - CloseIconTexture.Width - 8, 8), closeIconColor);
        }

        public override void CountDownCloseTimer(GameTime gameTime)
        {
            forceCloseTimer -= gameTime.ElapsedGameTime.Milliseconds;
            if (forceCloseTimer <= 0) { Close(); }
        }

        public override void CancelCloseTimer()
        {
            forceCloseTimer = DefaultForceCloseTime;
        }

        public override void SetBalloon(ClientBalloon balloon)
        {
            visibleBalloon = balloon;

            // When a new visible balloon is set, kick off the initialisation stuff
            if (visibleBalloon != null) { initialise(); }
        }

        private void initialise()
        {
            // Reset timers
            closeTimer = Configuration.MessageDisplayTime;
            forceCloseTimer = DefaultForceCloseTime;

            // Wrap the text
            wrappedTitle = TextUtility.wrapText(TitleFont, visibleBalloon.Label.Replace('\n', ' '), new Vector2(750, 64));
            wrappedContent = TextUtility.wrapText(ContentFont, visibleBalloon.Content, new Vector2(750, 430));

            // Get the images from the cache
            if (!balloonTextureCache.ContainsKey(visibleBalloon.ID))
            {
                BalloonContentCache cacheEntry = new BalloonContentCache(visibleBalloon.ID);
                ThreadPool.QueueUserWorkItem(o =>
                {
                    cacheEntry.QRCode = String.IsNullOrEmpty(visibleBalloon.Url) ? null : ImageGenerator.GenerateQRCode(graphics.GraphicsDevice, visibleBalloon.Url);
                    cacheEntry.Image = ImageGenerator.GenerateFromWeb(graphics.GraphicsDevice, visibleBalloon.ImageUrl);
                });

                balloonTextureCache.Add(visibleBalloon.ID, cacheEntry);
            }

            visibleBalloon.BalloonContentCache = balloonTextureCache[visibleBalloon.ID];
        }
    }
}
