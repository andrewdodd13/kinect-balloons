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
using BubblesClient.Input.Controllers.Kinect;
using BubblesClient.Input.Controllers.Mouse;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Dynamics.Joints;
using Balloons.DummyClient;
using Balloons.Messaging.Model;

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

        // Textures
        private Texture2D handTexture, balloonTexture;

        // Network
        public ScreenManager ScreenManager { get; private set; }

        // XNA Graphics
        private GraphicsDeviceManager graphics;
        private Vector2 screenDimensions;
        private SpriteBatch spriteBatch;

        // Input
        private IInputController input;
        private Dictionary<Hand, BodyJointPair> handBodies = new Dictionary<Hand, BodyJointPair>();

        // Physics World
        private World _world;
        private const float MeterInPixels = 64f;
        private Dictionary<int, ClientBalloon> balloons = new Dictionary<int, ClientBalloon>();

        public BubblesClientGame(ScreenManager screenManager)
        {
            // Initialise Graphics
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 768;
            graphics.PreferredBackBufferWidth = 1366;

            screenDimensions = new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

            // Initialise Input
            // Use this line to enable the Kinect
            //_input = new KinectControllerInput();

            // And this one to enable the Mouse (if you use both, Mouse is used)
            input = new MouseInput();
            input.Initialize(screenDimensions);

            // Initialise Content
            Content.RootDirectory = "Content";

            // Initialise Physics
            _world = new World(new Vector2(0, -2));

            // Initialise network
            this.ScreenManager = screenManager;
            ScreenManager.NewBalloonEvent += this.OnNewBalloon;
            ScreenManager.PopBalloonEvent += this.OnPopBalloon;
        }

        public void OnNewBalloon(object sender, EventArgs e)
        {
            // Cast out the message because I'm too lazy to properly make a delegate
            NewBalloonMessage m = (NewBalloonMessage)((MessageEventArgs)e).Message;

            // Choose where to place the balloon
            Vector2 position = new Vector2();
            switch (m.Direction)
            {
                case Direction.Left:
                    position.X = balloonTexture.Width * -1;
                    break;
                case Direction.Right:
                    position.X = balloonTexture.Width + screenDimensions.X;
                    break;

                case Direction.Any:
                default:
                    position.X = new Random().Next((int)screenDimensions.X);
                    break;
            }

            position.Y = m.Y * screenDimensions.Y;

            // Setup the balloon's body. 
            Body balloonBody = BodyFactory.CreateCircle(_world, 128f / (2f * MeterInPixels), 1f, PixelToWorld(position));
            balloonBody.BodyType = BodyType.Dynamic;
            balloonBody.Restitution = 0.3f;
            balloonBody.Friction = 0.5f;
            balloonBody.LinearDamping = 1.0f;

            Vector2 velocity = new Vector2(m.Velocity.X, 0);
            balloonBody.ApplyLinearImpulse(velocity * balloonBody.Mass);

            ClientBalloon b = new ClientBalloon(m.BalloonID, balloonBody);
            balloons.Add(b.ID, b);
        }

        public void OnPopBalloon(object sender, EventArgs e)
        {
            PopBalloonMessage m = (PopBalloonMessage)((MessageEventArgs)e).Message;
            Console.WriteLine("Pop balloon!");
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

            // Always do this last
            this.ScreenManager.Connect();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            handTexture = Content.Load<Texture2D>("tmpCircle");
            balloonTexture = Content.Load<Texture2D>("balloon");

            // Lol roof!
            Body _roofBody;
            _roofBody = BodyFactory.CreateRectangle(_world, screenDimensions.X * 4 / MeterInPixels, 1 / MeterInPixels, 1f, new Vector2(screenDimensions.X / 2 / MeterInPixels, 0));
            _roofBody.IsStatic = true;
            _roofBody.Restitution = 0.3f;
            _roofBody.Friction = 1f;

            _roofBody = BodyFactory.CreateRectangle(_world, screenDimensions.X * 4 / MeterInPixels, 1 / MeterInPixels, 1f, new Vector2(screenDimensions.X / 2 / MeterInPixels, screenDimensions.Y / MeterInPixels));
            _roofBody.IsStatic = true;
            _roofBody.Restitution = 0.3f;
            _roofBody.Friction = 1f;
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
            // Query the Network Manager for events
            //PerformNetworkEvents();

            // Query the Input Library
            this.HandleInput();

            _world.Step((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f);

            // Check if any of the balloons have buggered off
            List<int> removals = new List<int>();

            foreach (KeyValuePair<int, ClientBalloon> balloon in balloons)
            {
                Vector2 balloonPosition = balloon.Value.Body.Position;

                // 1.5 width for that extra bit of margin
                if (balloonPosition.X < (balloonTexture.Width * -1.5) / MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * MeterInPixels) / screenDimensions.Y;
                    ScreenManager.MoveBalloonOffscreen(balloon.Value, Direction.Left, exitHeight, balloon.Value.Body.LinearVelocity);
                    removals.Add(balloon.Key);
                }
                else if (balloonPosition.X > (balloonTexture.Width * 1.5 + screenDimensions.X) / MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * MeterInPixels) / screenDimensions.Y;
                    ScreenManager.MoveBalloonOffscreen(balloon.Value, Direction.Right, exitHeight, balloon.Value.Body.LinearVelocity);
                    removals.Add(balloon.Key);
                    _world.RemoveBody(balloon.Value.Body);
                }
            }

            removals.ForEach(x => balloons.Remove(x));

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.LightBlue);

            spriteBatch.Begin();

            // Draw all of the registered hands
            foreach (KeyValuePair<Hand, BodyJointPair> handBody in handBodies)
            {
                Vector2 cursorPos = WorldBodyToPixel(handBody.Value.Body.Position, new Vector2(handTexture.Width, handTexture.Height));
                spriteBatch.Draw(handTexture, cursorPos, Color.White);
            }

            // Draw all of the balloons
            foreach (KeyValuePair<int, ClientBalloon> balloon in balloons)
            {
                spriteBatch.Draw(balloonTexture, WorldBodyToPixel(balloon.Value.Body.Position, new Vector2(balloonTexture.Width, balloonTexture.Height)), Color.White);
                Console.WriteLine("Balloon Position: " + balloon.Value.Body.Position);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void HandleInput()
        {
            Hand[] hands = input.GetHandPositions();

            // Go through the hands array looking for new hands, if we find any, register them
            foreach (Hand hand in hands)
            {
                if (!handBodies.ContainsKey(hand))
                {
                    this.CreateHandFixture(hand);
                }
            }

            // Deregister any hands which aren't there any more
            List<Hand> _removals = new List<Hand>(handBodies.Keys.Except(hands));
            foreach (Hand hand in _removals)
            {
                this.RemoveHandFixture(hand);
            }

            // Move joint parts
            foreach (KeyValuePair<Hand, BodyJointPair> handBody in handBodies)
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

            handBodies.Add(hand, new BodyJointPair() { Body = handBody, Joint = handJoint });

            _world.AddJoint(handJoint);
        }

        private void RemoveHandFixture(Hand hand)
        {
            BodyJointPair bodyJoint = handBodies[hand];
            _world.RemoveJoint(bodyJoint.Joint);
            _world.RemoveBody(bodyJoint.Body);
            handBodies.Remove(hand);
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
            return (worldPosition * MeterInPixels) - (pixelOffset / 2);
        }

        private Vector2 PixelToWorldBody(Vector2 pixelPosition, Vector2 pixelOffset)
        {
            return (pixelPosition / MeterInPixels) + ((pixelOffset / MeterInPixels) / 2);
        }
    }
}
