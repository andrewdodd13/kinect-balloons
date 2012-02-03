using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BubblesServer;

namespace DummyClient
{
    public class DummyClient : Game
    {
        ScreenManager screen;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D balloonTexture;
        float balloonScale;

        public static void Main(String[] args)
        {
            DummyClient c = new DummyClient();
            c.Run();
        }

        public DummyClient()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            screen = new ScreenManager();
            screen.BalloonMapChanged += screen_BalloonMapChanged;
        }

        void screen_BalloonMapChanged(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            screen.Connect(IPAddress.Loopback, 4000);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            balloonTexture = LoadTexture("../../Balloon.jpg");
            balloonScale = 0.1f;
        }

        private Texture2D LoadTexture(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                return Texture2D.FromStream(graphics.GraphicsDevice, fs);
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            balloonTexture.Dispose();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Update the balloons' positions acording to their current velocity
            foreach(Balloon b in screen.Balloons.Values)
            {
                b.Pos += b.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

                // detect balloons leaving the screen
                if(b.Pos.X < 0.0f || b.Pos.X > 1.0f)
                {
                    screen.MoveBalloonOffscreen(b);
                }
            }
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            Vector2 screenSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Vector2 texSize = new Vector2(balloonTexture.Width, balloonTexture.Height);
            foreach(Balloon b in screen.Balloons.Values)
            {
                // transform the balloon position to screen coordinates
                // offset the position by half the texture size (at the center of the balloon)
                Vector2 balloonPos = (b.Pos * screenSize) - (texSize * balloonScale * 0.5f);
                spriteBatch.Draw(balloonTexture, balloonPos, null, Color.White, 0,
                    new Vector2(), balloonScale, SpriteEffects.None, 0);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
