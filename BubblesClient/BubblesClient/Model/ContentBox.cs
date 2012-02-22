namespace BubblesClient.Model
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Balloons.Messaging.Model;
    using BubblesClient.Utility;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// The ContentBox manages the display of an article on screen as well as
    /// related functionality such as the timed removal and user's ability to
    /// close the box.
    /// </summary>
    public class ContentBox
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

        // Called when the box is closed for any reason
        public event EventHandler OnClose;

        private ClientBalloon _visibleBalloon;
        public ClientBalloon VisibleBalloon
        {
            get { return _visibleBalloon; }
            set
            {
                _visibleBalloon = value;

                // When a new visible balloon is set, kick off the initialisation stuff
                if (_visibleBalloon != null) { initialise(); }
            }
        }

        public ContentBox(Vector2 screenDimensions, GraphicsDeviceManager graphics)
        {
            this.screenDimensions = screenDimensions;
            this.graphics = graphics;
        }

        public void Update(GameTime gameTime)
        {
            closeTimer -= gameTime.ElapsedGameTime.Milliseconds;
            if (closeTimer < 0) { Close(); }
        }

        /// <summary>
        /// Call this if and only if the VisibleBalloon property is not null.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
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
            if (VisibleBalloon.Votes > 0) { votesColor = Color.Green; }
            else if (VisibleBalloon.Votes < 0) { votesColor = Color.Red; }

            String votesLabel = (VisibleBalloon.Votes > 0 ? "+" : "") + VisibleBalloon.Votes;
            TextUtility.drawTextLabel(spriteBatch, TitleFont, votesLabel, position + new Vector2(754 - TitleFont.MeasureString(votesLabel).X, 0), votesColor);

            // Draw the QR Code
            if (VisibleBalloon.BalloonContentCache.QRCode != null)
            {
                spriteBatch.Draw(VisibleBalloon.BalloonContentCache.QRCode, position + new Vector2(BoxTexture.Width - 24 - 224 - 9, BoxTexture.Height - 224 - 24 - 9), Color.White);
            }
            else
            {
                spriteBatch.Draw(LoadingSprite, position + new Vector2(BoxTexture.Width - 24 - 224 - 9, BoxTexture.Height - 224 - 24 - 9), Color.White);
            }

            // Draw the Image
            Texture2D balloonImage = VisibleBalloon.BalloonContentCache.Image;
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

        public void CountDownCloseTimer(GameTime gameTime)
        {
            forceCloseTimer -= gameTime.ElapsedGameTime.Milliseconds;
            if (forceCloseTimer <= 0) { Close(); }
        }

        public void CancelCloseTimer()
        {
            forceCloseTimer = DefaultForceCloseTime;
        }

        public BalloonContentCache GetBalloonContent(string balloonID)
        {
            BalloonContentCache cacheEntry = null;
            if(!balloonTextureCache.TryGetValue(balloonID, out cacheEntry))
            {
                cacheEntry = new BalloonContentCache(balloonID);
                balloonTextureCache.Add(balloonID, cacheEntry);
            }
            return cacheEntry;
        }

        private void initialise()
        {
            // Reset timers
            closeTimer = Configuration.MessageDisplayTime;
            forceCloseTimer = DefaultForceCloseTime;

            // Wrap the text
            wrappedTitle = TextUtility.wrapText(TitleFont, VisibleBalloon.Label.Replace('\n', ' '), new Vector2(750, 64));
            wrappedContent = TextUtility.wrapText(ContentFont, VisibleBalloon.Content, new Vector2(750, 430));

            // Get the images from the cache or generate them
            BalloonContentCache cacheEntry = GetBalloonContent(_visibleBalloon.ID);
            if(cacheEntry.Image == null)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    System.Drawing.Bitmap img = ImageGenerator.GenerateFromWeb(_visibleBalloon.ImageUrl);
                    cacheEntry.Image = ImageGenerator.BitmapToTexture(img, graphics.GraphicsDevice);
                });
            }
            if((cacheEntry.QRCode == null) && !String.IsNullOrEmpty(_visibleBalloon.Url))
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    System.Drawing.Bitmap img = ImageGenerator.GenerateQRCode(_visibleBalloon.Url);
                    cacheEntry.QRCode = ImageGenerator.BitmapToTexture(img, graphics.GraphicsDevice);
                });
            }
            _visibleBalloon.BalloonContentCache = cacheEntry;
        }

        private void Close()
        {
            this._visibleBalloon = null;
            if (OnClose != null) { OnClose(this, null); }
        }
    }
}
