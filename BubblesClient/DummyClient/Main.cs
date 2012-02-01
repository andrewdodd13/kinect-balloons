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
            // TODO: Add your update logic here
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
            foreach(Balloon b in screen.Balloons.Values)
            {
                spriteBatch.Draw(balloonTexture, b.Pos, null, Color.White, 0,
                    new Vector2(), balloonScale, SpriteEffects.None, 0);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
