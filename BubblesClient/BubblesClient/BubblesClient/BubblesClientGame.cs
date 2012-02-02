using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using BubblesClient.Input.Controllers;
using BubblesClient.Network;
using BubblesClient.Model;
using BubblesClient.Network.Event;
using BubblesClient.Input.Controllers.Kinect;
using BubblesClient.Input.Controllers.Mouse;

namespace BubblesClient
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BubblesClientGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Color backgroundColour = Color.Blue;

        IInputController _input;
        INetworkEventManager _networkEvents = new MockNetworkManager();

        Texture2D texture;

        public BubblesClientGame()
        {
            graphics = new GraphicsDeviceManager(this);

            // Use this line to enable the Kinect
            //_input = new KinectControllerInput();

            // And this one to enable the Mouse (if you use both, Mouse is used)
            _input = new MouseInput();

            _input.Initialize(new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            texture = Content.Load<Texture2D>("tmpCircle");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // Query the Network Manager for events
            //PerformNetworkEvents();

            //if (_input.SwipeLeftControl == ButtonState.Pressed)
            //{
            //    if(backgroundColour == Color.Red) 
            //    {
            //        backgroundColour = Color.Green;
            //    }
            //    else if (backgroundColour == Color.Green)
            //    {
            //        backgroundColour = Color.Blue;
            //    }
            //    else
            //    {
            //        backgroundColour = Color.Red;
            //    }
            //}

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(backgroundColour);

            spriteBatch.Begin();
            Vector3[] hands = _input.GetHandPositions();
            for(int i = 0; i < hands.Length; i++) {
                Vector2 pos = new Vector2(hands[i].X, hands[i].Y);
                spriteBatch.Draw(texture, pos, Color.White);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Helper method to perform actions for any network events which have
        /// occurred since the last frame. This involves destroying balloons 
        /// which the server has decided are too old, and adding any new 
        /// balloons we have been sent.
        /// </summary>
        protected void PerformNetworkEvents()
        {
            // Firstly, pop any balloons that need to be popped
            List<Balloon> poppedBalloons = _networkEvents.GetPoppedBalloons();
            poppedBalloons.ForEach(x => PopBalloon(x));

            // Then add any new balloons that we have been sent
            List<NewBalloonEvent> newBalloons = _networkEvents.GetNewBalloons();
            newBalloons.ForEach(x => AddBalloon(x));
        }

        /// <summary>
        /// Adds a new balloon to the screen. The New Balloon Event describes
        /// details of the balloon and also the initial position and velocity
        /// of the balloon.
        /// </summary>
        /// <param name="newBalloonEvent">The New Balloon Event describing the
        /// Balloon to add.</param>
        protected void AddBalloon(NewBalloonEvent newBalloonEvent)
        {

        }

        /// <summary>
        /// Pops the specified Balloon. This will cause an animation to be 
        /// started and remove the Balloon from the internal list of balloons 
        /// on this client.
        /// </summary>
        /// <param name="balloon">The balloon to pop</param>
        protected void PopBalloon(Balloon balloon)
        {
        }
    }
}
