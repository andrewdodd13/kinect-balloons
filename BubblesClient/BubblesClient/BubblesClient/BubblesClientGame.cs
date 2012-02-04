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
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Dynamics.Joints;

namespace BubblesClient
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BubblesClientGame : Microsoft.Xna.Framework.Game
    {
        private class BodyJointPair
        {
            public Body Body { get; set; }
            public Joint Joint { get; set; }
        }

        SpriteBatch spriteBatch;
        Color backgroundColour = Color.Blue;

        Texture2D texture, _balloon;

        private Body _roofBody, _balloonBody;

        // XNA Graphics
        private GraphicsDeviceManager _graphics;
        private Vector2 _screenDimensions;

        // Input
        private IInputController _input;
        private Dictionary<Hand, BodyJointPair> _handBodies = new Dictionary<Hand, BodyJointPair>();

        private INetworkEventManager _networkEvents = new MockNetworkManager();

        // Physics World
        private World _world;
        private const float MeterInPixels = 64f;

        public BubblesClientGame()
        {
            // Initialise Graphics
            _graphics = new GraphicsDeviceManager(this);

            _screenDimensions = new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

            // Initialise Input
            // Use this line to enable the Kinect
            //_input = new KinectControllerInput();

            // And this one to enable the Mouse (if you use both, Mouse is used)
            _input = new MouseInput();
            _input.Initialize(_screenDimensions);

            // Initialise Content
            Content.RootDirectory = "Content";

            // Initialise Physics
            _world = new World(new Vector2(0, -2));
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // Initialise base
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

            texture = Content.Load<Texture2D>("tmpCircle");
            _balloon = Content.Load<Texture2D>("balloon");

            // Setup a balloon
            Vector2 balloonPosition = (_screenDimensions / 2) / MeterInPixels;
            _balloonBody = BodyFactory.CreateCircle(_world, 128f / (2f * MeterInPixels), 1f, PixelToWorld(_screenDimensions / 2));
            _balloonBody.BodyType = BodyType.Dynamic;
            _balloonBody.Restitution = 0.3f;
            _balloonBody.Friction = 0.5f;

            // Lol roof!
            _roofBody = BodyFactory.CreateRectangle(_world, _screenDimensions.X / MeterInPixels, 1 / MeterInPixels, 1f, new Vector2(_screenDimensions.X / 2 / MeterInPixels, 0));
            _roofBody.IsStatic = true;
            _roofBody.Restitution = 0.3f;
            _roofBody.Friction = 0.5f;
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

            // Query the Input Library
            this.HandleInput();

            _world.Step((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f);

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

            // Update the position of all the registered hands
            foreach (KeyValuePair<Hand, BodyJointPair> handBody in _handBodies)
            {
                Vector2 cursorPos = WorldBodyToPixel(handBody.Value.Body.Position, new Vector2(64, 64));
                spriteBatch.Draw(texture, cursorPos, Color.White);
            }

            spriteBatch.Draw(_balloon, WorldBodyToPixel(_balloonBody.Position, new Vector2(128, 128)), Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void HandleInput()
        {
            Hand[] hands = _input.GetHandPositions();

            // Go through the hands array looking for new hands, if we find any, register them
            foreach (Hand hand in hands)
            {
                if (!_handBodies.ContainsKey(hand))
                {
                    this.CreateHandFixture(hand);
                }
            }

            // Deregister any hands which aren't there any more
            foreach (Hand hand in _handBodies.Keys.Except(hands))
            {
                this.RemoveHandFixture(hand);
            }

            // Move joint parts
            foreach (KeyValuePair<Hand, BodyJointPair> handBody in _handBodies)
            {
                handBody.Value.Joint.WorldAnchorB = new Vector2(handBody.Key.Position.X, handBody.Key.Position.Y) / MeterInPixels;
            }
        }

        private void CreateHandFixture(Hand hand)
        {
            Vector2 handPos = new Vector2(hand.Position.X, hand.Position.Y);
            Body handBody = BodyFactory.CreateRectangle(_world, 1f, 1f, 1f, handPos / MeterInPixels);
            handBody.BodyType = BodyType.Dynamic;
            FixedMouseJoint handJoint = new FixedMouseJoint(handBody, handBody.Position);
            handJoint.MaxForce = 1000f;

            _handBodies.Add(hand, new BodyJointPair() { Body = handBody, Joint = handJoint });

            _world.AddJoint(handJoint);
        }

        private void RemoveHandFixture(Hand hand)
        {
            BodyJointPair bodyJoint = _handBodies[hand];
            _world.RemoveJoint(bodyJoint.Joint);
            _world.RemoveBody(bodyJoint.Body);
            _handBodies.Remove(hand);
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

        private Vector2 WorldToPixel(Vector2 worldPosition)
        {
            return worldPosition * MeterInPixels;
        }

        private Vector2 PixelToWorld(Vector2 pixelPosition)
        {
            return pixelPosition / MeterInPixels;
        }

        private Vector2 WorldBodyToPixel(Vector2 worldPosition, Vector2 pixelOffset)
        {
            return worldPosition * MeterInPixels - (pixelOffset / 2);
        }

        private Vector2 PixelToWorldBody(Vector2 pixelPosition, Vector2 pixelOffset)
        {
            return pixelPosition / MeterInPixels + ((pixelOffset / MeterInPixels) / 2);
        }
    }
}
