namespace BubblesClient.Model
{
    using System;
    using BubblesClient.Model.ContentBox;
    using BubblesClient.Utility;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;

    public class ManualContentBox : AbstractContentBox
    {
        // Currently wrapped texts
        private string wrappedTitle;
        private string wrappedContent;

        private SpriteFont ContentFont { get; set; }
        private SpriteFont TitleFont { get; set; }
        private Texture2D BoxTexture { get; set; }
        private Texture2D LoadingSprite { get; set; }

        public ManualContentBox(Vector2 screenDimensions, GraphicsDeviceManager graphicsManager) :
            base(screenDimensions, graphicsManager)
        {
        }

        public override void LoadResources(ContentManager contentManager)
        {
            base.LoadResources(contentManager);

            this.TitleFont = contentManager.Load<SpriteFont>("Fonts/Content-Title");
            this.ContentFont = contentManager.Load<SpriteFont>("Fonts/SpriteFontSmall");
            this.BoxTexture = contentManager.Load<Texture2D>("Images/ContentBox");
            this.LoadingSprite = contentManager.Load<Texture2D>("Images/LoadingSprite");
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
            if (visibleBalloon.BalloonContentCache[CacheType.QRCode] != null)
            {
                spriteBatch.Draw(visibleBalloon.BalloonContentCache[CacheType.QRCode, graphicsManager.GraphicsDevice], position + new Vector2(BoxTexture.Width - 24 - 224 - 9, BoxTexture.Height - 224 - 24 - 9), Color.White);
            }
            else
            {
                spriteBatch.Draw(LoadingSprite, position + new Vector2(BoxTexture.Width - 24 - 224 - 9, BoxTexture.Height - 224 - 24 - 9), Color.White);
            }

            // Draw the Image
            Texture2D balloonImage = visibleBalloon.BalloonContentCache[CacheType.WebImage, graphicsManager.GraphicsDevice];
            if (balloonImage != null)
            {
                spriteBatch.Draw(balloonImage, position + new Vector2(BoxTexture.Width - 24 - 112 - (balloonImage.Width / 2) - 9, 24 + 112 - (balloonImage.Height / 2)), Color.White);
            }
            else
            {
                spriteBatch.Draw(LoadingSprite, position + new Vector2(BoxTexture.Width - 24 - LoadingSprite.Width - 9, 24), Color.White);
            }

            DrawCloseTimer(spriteBatch);
        }

        protected override void Initialise()
        {
            base.Initialise();

            // Wrap the text
            wrappedTitle = TextUtility.wrapText(TitleFont, visibleBalloon.Label.Replace('\n', ' '), new Vector2(750, 64));
            wrappedContent = TextUtility.wrapText(ContentFont, visibleBalloon.Content, new Vector2(750, 430));
        }
    }
}