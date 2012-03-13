namespace BubblesClient.Model.ContentBox
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using BubblesClient.Utility;
    using System.Collections.Generic;
    using Balloons.Messaging.Model;
    using System.Threading;

    /// <summary>
    /// The ContentBox manages the display of an article on screen as well as
    /// related functionality such as the timed removal and user's ability to
    /// close the box.
    /// </summary>
    public abstract class AbstractContentBox
    {
        #region "Member fields"
        // Graphics
        protected Vector2 screenDimensions;
        protected GraphicsDeviceManager graphicsManager;

        // Texture cache
        private Dictionary<string, BalloonContentCache> balloonTextureCache = new Dictionary<string, BalloonContentCache>();

        // Timers
        protected float closeTimer = Configuration.MessageDisplayTime;
        protected const float DefaultForceCloseTime = 500.0f;
        protected float forceCloseTimer = DefaultForceCloseTime;
        protected long internalTimer = 0;

        // Shared textures
        protected Texture2D closeIconTexture;
        protected List<Texture2D> countDownImages;
        #endregion

        #region "Abstract & Virtual Methods"
        public abstract void Draw(SpriteBatch spriteBatch);

        public virtual void GenerateCaption(ClientBalloon balloon) { }
        public virtual void GenerateTextContent(ClientBalloon balloon) { }
        public virtual void GeneratePlaneCaption(ClientPlane plane) { }
        #endregion

        // Raised when the box is closed for any reason
        public event EventHandler OnClose;

        public AbstractContentBox(Vector2 screenDimensions, GraphicsDeviceManager graphicsManager)
        {
            this.screenDimensions = screenDimensions;
            this.graphicsManager = graphicsManager;
        }

        /// <summary>
        /// Updates the internal close timers
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime)
        {
            closeTimer -= gameTime.ElapsedGameTime.Milliseconds;
            internalTimer += gameTime.ElapsedGameTime.Milliseconds;
            if (closeTimer < 0) { Close(); }
        }

        /// <summary>
        /// Loads the base resources; when overriding make sure to make a base 
        /// call to this method.
        /// </summary>
        /// <param name="contentManager"></param>
        public virtual void LoadResources(ContentManager contentManager)
        {
            closeIconTexture = contentManager.Load<Texture2D>("Images/CloseIcon");
            countDownImages = new List<Texture2D>();
            for (int i = 0; i <= 30; i++)
            {
                countDownImages.Add(contentManager.Load<Texture2D>("Images/Countdown/" + i));
            }
        }

        /// <summary>
        /// Initialises the Content Box for a new balloon.
        /// </summary>
        protected virtual void Initialise()
        {
            // Reset timers
            closeTimer = Configuration.MessageDisplayTime;
            forceCloseTimer = DefaultForceCloseTime;

            // Get the images from the cache or generate them
            GetBalloonContent(visibleBalloon.ID);
            ThreadPool.QueueUserWorkItem(o => GenerateQR(visibleBalloon));
            ThreadPool.QueueUserWorkItem(o => GenerateImage(visibleBalloon));
        }

        protected ClientBalloon visibleBalloon;
        public bool IsVisible
        {
            get { return visibleBalloon != null; }
        }

        public void SetBalloon(ClientBalloon balloon)
        {
            visibleBalloon = balloon;

            // When a new visible balloon is set, kick off the initialisation stuff
            if (visibleBalloon != null) { Initialise(); }
        }

        public BalloonContentCache GetBalloonContent(string balloonID)
        {
            BalloonContentCache cacheEntry = null;
            if (!balloonTextureCache.TryGetValue(balloonID, out cacheEntry))
            {
                cacheEntry = new BalloonContentCache(balloonID);
                balloonTextureCache.Add(balloonID, cacheEntry);
            }
            return cacheEntry;
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

        protected void DrawCloseTimer(SpriteBatch spriteBatch)
        {
            // Draw the timer
            // Animate timer: make the texture bigger/smaller over time through scale
            float timerScale = 1.0f + 0.1f * (float)Math.Sin(internalTimer / 500f);
            
            Texture2D currentFrame = countDownImages[(int)(closeTimer / 1000)];
            Vector2 position = screenDimensions - new Vector2(currentFrame.Width + 8, currentFrame.Height + 8);
            Rectangle textureRect = new Rectangle((int)position.X, (int)position.Y, currentFrame.Width, currentFrame.Height);

            // Scale the texture rectangle at its center and not at its top-left corner
            // like Draw() does when you pass a scaling factor.
            float newWidth = (textureRect.Width * timerScale);
            float newHeight = (textureRect.Height * timerScale);
            float newX = (textureRect.Center.X - newWidth * 0.5f);
            float newY = (textureRect.Center.Y - newHeight * 0.5f);
            textureRect = new Rectangle((int)newX, (int)newY, (int)newWidth, (int)newHeight);

            spriteBatch.Draw(currentFrame, textureRect, Color.White);

            // Draw the close icon
            Color closeIconColor = Color.White;
            closeIconColor.A = (byte)((0.25 + (0.75 * ((DefaultForceCloseTime - forceCloseTimer) / DefaultForceCloseTime))) * 255);
            spriteBatch.Draw(closeIconTexture, new Vector2(screenDimensions.X - closeIconTexture.Width - 8, 8), closeIconColor);
            spriteBatch.Draw(closeIconTexture, new Vector2(8, 8), closeIconColor);
        }

        protected virtual void Close()
        {
            this.visibleBalloon = null;
            if (OnClose != null) { OnClose(this, null); }
        }

        protected void GenerateQR(ClientBalloon balloon)
        {
            BalloonContentCache cacheEntry = balloon.BalloonContentCache;
            bool imageUpdated = false;
            if ((cacheEntry[CacheType.QRCode] == null) && !String.IsNullOrEmpty(balloon.Url))
            {
                cacheEntry[CacheType.QRCode] = ImageGenerator.GenerateQRCode(balloon.Url);
                imageUpdated = true;
            }

            if (imageUpdated || cacheEntry[CacheType.Content] == null)
            {
                GenerateTextContent(balloon);
            }
        }

        protected void GenerateImage(ClientBalloon balloon)
        {
            BalloonContentCache cacheEntry = balloon.BalloonContentCache;
            bool imageUpdated = false;
            if (cacheEntry[CacheType.WebImage] == null)
            {
                cacheEntry[CacheType.WebImage] = ImageGenerator.GenerateFromWeb(balloon.ImageUrl);
                imageUpdated = true;
            }

            if (imageUpdated || cacheEntry[CacheType.Content] == null)
            {
                GenerateTextContent(balloon);
            }
        }
    }
}
