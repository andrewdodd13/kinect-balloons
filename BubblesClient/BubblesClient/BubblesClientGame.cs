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
using Balloons.Messaging;
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

        private class Bucket
        {
            public int ID;
            public Vector2 position, size;
            public Body physicalBody;
        }

        // Textures
        private Texture2D handTexture, balloonTexture, bucketTexture;

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
        private List<Bucket> buckets = new List<Bucket>();

        public int temp = 0;

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
        }

        public void ProcessNetworkMessages()
        {
            List<Message> messages = ScreenManager.MessageQueue.DequeueAll();
            foreach(Message msg in messages)
            {
                if(msg == null)
                {
                    // the connection to the server was closed
                    break;
                }

                switch(msg.Type)
                {
                case MessageType.NewBalloon:
                    OnNewBalloon((NewBalloonMessage)msg);
                    break;
                case MessageType.PopBalloon:
                    OnPopBalloon((PopBalloonMessage)msg);
                    break;
                }
            }
        }

        public void OnNewBalloon(NewBalloonMessage m)
        {
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

            Vector2 velocity = new Vector2(m.Velocity.X, m.Velocity.Y);
            balloonBody.ApplyLinearImpulse(velocity * balloonBody.Mass);

            ClientBalloon b = new ClientBalloon(m.BalloonID, balloonBody);
            balloons.Add(b.ID, b);
        }

        public void OnPopBalloon(PopBalloonMessage m)
        {
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
            bucketTexture = Content.Load<Texture2D>("tmpBucket");

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

            //Load buckets
            //Note to self: Prettify - William
            for (int i = 0; i < 4; i++)
            {
                Bucket b = new Bucket();
                b.ID = i;
                b.position = new Vector2(3+5*i, PixelToWorld(new Vector2(0, 800)).Y);
                b.size = PixelToWorld(new Vector2(bucketTexture.Width, bucketTexture.Height));
                b.physicalBody = BodyFactory.CreateRectangle(_world, b.size.X, b.size.Y, 0.1f, b.position);
                buckets.Add(b);
                b.physicalBody.OnCollision += new OnCollisionEventHandler(bucketCollision);
            }
        }

        bool bucketCollision(Fixture fixtureA, Fixture fixtureB, FarseerPhysics.Dynamics.Contacts.Contact contact)
        {
            //I assume that fixtureA is always the bucket object (which is detecting the collision)
            Bucket _bucket = null;
            foreach (Bucket b in buckets)
            {
                if (fixtureA.Body == b.physicalBody)
                {
                    _bucket = b;
                    break;
                }
            }
            //Try to find fixtureB in the balloons
            foreach (ClientBalloon b in balloons.Values)
            {
                if (fixtureB.Body == b.Body)
                {
                    ApplyBucketToBalloon(_bucket, b);
                    return true; //set this to false to allow the ballon to pass through the bucket
                }
            }
            
            return false; //let hand objects pass through
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
            ProcessNetworkMessages();

            // Query the Input Library
            this.HandleInput();

            _world.Step((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f);

            // Check if any of the balloons have buggered off
            List<ClientBalloon> removals = new List<ClientBalloon>();

            foreach (ClientBalloon balloon in balloons.Values)
            {
                Vector2 balloonPosition = balloon.Body.Position;

                // 1.5 width for that extra bit of margin
                if (balloonPosition.X < (balloonTexture.Width * -1.5) / MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * MeterInPixels) / screenDimensions.Y;
                    ScreenManager.MoveBalloonOffscreen(balloon, Direction.Left, exitHeight, balloon.Body.LinearVelocity);
                    removals.Add(balloon);
                }
                else if (balloonPosition.X > (balloonTexture.Width * 1.5 + screenDimensions.X) / MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * MeterInPixels) / screenDimensions.Y;
                    ScreenManager.MoveBalloonOffscreen(balloon, Direction.Right, exitHeight, balloon.Body.LinearVelocity);
                    removals.Add(balloon);
                }
            }

            removals.ForEach(x => balloons.Remove(x.ID));
            removals.ForEach(x => _world.RemoveBody(x.Body));

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
                //Console.WriteLine("Balloon Position: " + balloon.Value.Body.Position);
            }

            //Draw all buckets
            foreach (Bucket bucket in buckets)
            {
                spriteBatch.Draw(bucketTexture, WorldBodyToPixel(bucket.position, WorldToPixel(bucket.size)), Color.White);
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

        private void ApplyBucketToBalloon(Bucket bucket, ClientBalloon balloon)
        {
            //Note to Lauren: Place your balloon bucket stuff here (in case the name wasn't descriptive enough :P)
            Console.WriteLine("Bucket {0} collided with ballon {1}", bucket.ID, balloon.ID);
        }
    }
}
