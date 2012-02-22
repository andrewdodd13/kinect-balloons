namespace BubblesClient.Model.ContentBox
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// The ContentBox manages the display of an article on screen as well as
    /// related functionality such as the timed removal and user's ability to
    /// close the box.
    /// </summary>
    public abstract class AbstractContentBox
    {
        public abstract void LoadResources(ContentManager contentManager);

        public abstract void Update(GameTime gameTime);

        public abstract void Draw(SpriteBatch spriteBatch);

        public abstract void SetBalloon(ClientBalloon balloon);

        public abstract void CountDownCloseTimer(GameTime gameTime);
        public abstract void CancelCloseTimer();

        // Called when the box is closed for any reason
        public event EventHandler OnClose;

        protected ClientBalloon visibleBalloon;
        public bool IsVisible
        {
            get { return visibleBalloon == null; }
        }

        protected void Close()
        {
            this.visibleBalloon = null;
            if (OnClose != null) { OnClose(this, null); }
        }
    }
}
