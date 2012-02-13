using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Balloons.DummyClient;
using Balloons.Messaging.Model;
using BubblesClient.Input.Controllers;
using BubblesClient.Input.Controllers.Mouse;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

        //Hmm, abstract?
        private class WorldObject
        {
            public enum objType { Balloon, Bucket, Hand };
            public objType type;
            public ClientBalloon balloon;
            public Bucket bucket;
            public Hand hand;
        }

        // Textures
        private Texture2D skyTexture, handTexture, contentBox;
        private Texture2D balloonWhite, balloonStripes, balloonSpots;
        private Texture2D balloonTwitterWhite, balloonTwitterStripes, balloonTwitterSpots;
        private Texture2D balloonNewsWhite, balloonNewsStripes, balloonNewsSpots;
        private Texture2D balloonCustomWhite, balloonCustomStripes, balloonCustomSpots; //TODO: add twitter and news balloon images
        private Texture2D boxTexture;
        private Texture2D bucketRed, bucketGreen, bucketBlue, bucketStripes, bucketSpots;
        private SpriteFont textContent, textSummary;

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
        private Dictionary<string, ClientBalloon> balloons = new Dictionary<string, ClientBalloon>();
        private List<Bucket> buckets = new List<Bucket>();

        //other stuff
        private bool showBuckets = true;
        private int oldBucketID = 5; //buckets 0-4

        // The time to display a message for, in milliseconds
        private const int MessageDisplayTime = 30000;
        private const float BalloonWidth = 162f;
        private const float BalloonHeight = 192f;

        // If this is not null then we will be showing a balloon. We really need
        // a state machine.
        private string poppedBalloonID = null;

        //Todo - find a better name for this, objects is quite vague
        private Dictionary<Body, WorldObject> objects = new Dictionary<Body, WorldObject>();

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
            foreach (Message msg in messages)
            {
                if (msg == null)
                {
                    // the connection to the server was closed
                    break;
                }

                switch (msg.Type)
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
                    position.X = balloonWhite.Width * -1;
                    break;
                case Direction.Right:
                    position.X = balloonWhite.Width + screenDimensions.X;
                    break;

                case Direction.Any:
                default:
                    position.X = new Random().Next((int)screenDimensions.X);
                    break;
            }

            position.Y = m.Y * screenDimensions.Y;

            // Setup the balloon's body.
            float circleRadius = BalloonWidth / (2f * MeterInPixels);

            Body balloonBody = BodyFactory.CreateCircle(_world, circleRadius, 1f, PixelToWorld(position));
            balloonBody.BodyType = BodyType.Dynamic;
            balloonBody.Restitution = 0.3f;
            balloonBody.Friction = 0.5f;
            balloonBody.LinearDamping = 1.0f;

            Vector2 velocity = new Vector2(m.Velocity.X, m.Velocity.Y);
            balloonBody.ApplyLinearImpulse(velocity * balloonBody.Mass);

            Balloon balloon = ScreenManager.GetBalloonDetails(m.BalloonID);
            ClientBalloon b = new ClientBalloon(balloon, balloonBody);
            b.Body.OnCollision += new OnCollisionEventHandler(onBalloonCollision);

            balloons.Add(b.ID, b);
            objects.Add(b.Body, new WorldObject { type = WorldObject.objType.Balloon, balloon = b }); //need to remember to remove this on pop
        }

        bool onBalloonCollision(Fixture fixtureA, Fixture fixtureB, FarseerPhysics.Dynamics.Contacts.Contact contact)
        {
            if (!objects.ContainsKey(fixtureA.Body))
            {
                Console.WriteLine("Error: a balloon was not in the objects dictionary");
                return true;
            }
            WorldObject A = objects[fixtureA.Body];
            if (A.type != WorldObject.objType.Balloon)
            {
                Console.WriteLine("Error: balloon collide event attached to non-balloon body");
                return true;
            }
            if (!objects.ContainsKey(fixtureB.Body))
            {
                //This is an acceptable case
                return true;
            }
            WorldObject B = objects[fixtureB.Body];

            if (B.type == WorldObject.objType.Bucket)
            {
                ApplyBucketToBalloon(B.bucket, A.balloon);
            }

            if (B.type == WorldObject.objType.Hand)
            {
                foreach (Hand altHand in handBodies.Keys)
                {
                    if (altHand != B.hand)
                    {
                        //Magic number! Might need to adjust for sensitivity
                        //Also, it might be worth checking the velocity/momentum of the hands to check they are converving on the balloon
                        if (Vector2.Distance(new Vector2(altHand.Position.X, altHand.Position.Y), WorldToPixel(fixtureA.Body.Position)) < 128)
                        {
                            PopBalloon(A.balloon.ID);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Handles the case where the server forces us to pop a balloon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            // TODO: Should this be the first line... or the second? :P
            float gapBetweenBuckets = (screenDimensions.Y - (bucketRed.Width * 5)) / 6;
            gapBetweenBuckets = 121;
            for (int i = 0; i < 5; i++)
            {
                Bucket b = new Bucket() { ID = i };
                float x = (i + 1) * gapBetweenBuckets + (i + 0.5f) * bucketRed.Width; //buckets 128x128
                float y = screenDimensions.Y - bucketRed.Height;
                b.position = PixelToWorld(new Vector2(x, y));
                b.size = PixelToWorld(new Vector2(bucketRed.Width, bucketRed.Height));
                b.physicalBody = BodyFactory.CreateRectangle(_world, b.size.X, b.size.Y, 0.1f, b.position);
                buckets.Add(b);
                objects.Add(b.physicalBody, new WorldObject { type = WorldObject.objType.Bucket, bucket = b });
            }

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

            textContent = Content.Load<SpriteFont>("Fonts/SpriteFontSmall");
            textSummary = Content.Load<SpriteFont>("Fonts/SpriteFontLarge");

            skyTexture = Content.Load<Texture2D>("Images/Sky");
            handTexture = Content.Load<Texture2D>("Images/Hand");
            contentBox = Content.Load<Texture2D>("Images/ContentBox");
            balloonCustomWhite = Content.Load<Texture2D>("Images/BalloonWhiteCustom");
            balloonCustomSpots = Content.Load<Texture2D>("Images/BalloonSpotsCustom");
            balloonCustomStripes = Content.Load<Texture2D>("Images/BalloonStripesCustom");
            balloonTwitterWhite = Content.Load<Texture2D>("Images/BalloonWhiteTwitter");
            balloonTwitterSpots = Content.Load<Texture2D>("Images/BalloonSpotsTwitter");
            balloonTwitterStripes = Content.Load<Texture2D>("Images/BalloonStripesTwitter");
            balloonNewsWhite = Content.Load<Texture2D>("Images/BalloonWhiteNews");
            balloonNewsSpots = Content.Load<Texture2D>("Images/BalloonSpotsNews");
            balloonNewsStripes = Content.Load<Texture2D>("Images/BalloonStripesNews");
            boxTexture = Content.Load<Texture2D>("Images/Box");
            balloonWhite = Content.Load<Texture2D>("Images/BalloonWhite");
            balloonStripes = Content.Load<Texture2D>("Images/BalloonStripes");
            balloonSpots = Content.Load<Texture2D>("Images/BalloonSpots");
            bucketRed = Content.Load<Texture2D>("Images/BucketRed");
            bucketGreen = Content.Load<Texture2D>("Images/bucketGreen");
            bucketBlue = Content.Load<Texture2D>("Images/bucketBlue");
            bucketStripes = Content.Load<Texture2D>("Images/bucketStripes");
            bucketSpots = Content.Load<Texture2D>("Images/bucketSpots");
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

            // Query the Input Library if there isn't currently a message displayed.
            if (poppedBalloonID == null)
            {
                this.HandleInput();
            }

            _world.Step((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f);

            // God this is hacky!
            MouseState mouseState = Mouse.GetState();
            if (mouseState.MiddleButton == ButtonState.Pressed)
            {
                poppedBalloonID = null;
            }

            // Check if any of the balloons have buggered off
            List<ClientBalloon> removals = new List<ClientBalloon>();

            foreach (ClientBalloon balloon in balloons.Values)
            {
                Vector2 balloonPosition = balloon.Body.Position;

                // 1.5 width for that extra bit of margin
                if (balloonPosition.X < (bucketRed.Width * -1.5) / MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * MeterInPixels) / screenDimensions.Y;
                    ScreenManager.MoveBalloonOffscreen(balloon, Direction.Left, exitHeight, balloon.Body.LinearVelocity);
                    removals.Add(balloon);
                }
                else if (balloonPosition.X > (bucketRed.Width * 1.5 + screenDimensions.X) / MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * MeterInPixels) / screenDimensions.Y;
                    ScreenManager.MoveBalloonOffscreen(balloon, Direction.Right, exitHeight, balloon.Body.LinearVelocity);
                    removals.Add(balloon);
                }
            }

            removals.ForEach(x =>
            {
                balloons.Remove(x.ID);
                _world.RemoveBody(x.Body);
            });

            //Show buckets if a balloon is in lower 1/3 of screen
            //I don't like how this is implemented - animation speed is dependant on frame rate
            //Would be better to have a spring or joint moving the buckets, but I haven't quite figured out how they work yet
            bool shouldShowBuckets = false;
            Vector2 screen = PixelToWorld(screenDimensions);
            foreach (ClientBalloon balloon in balloons.Values)
            {
                //if (balloon.Body.Position.Y > screen.Y * 2 / 3)
                if (balloon.Body.Position.Y + (128f / MeterInPixels) > screen.Y * 2 / 3)
                {
                    shouldShowBuckets = true;
                    break;
                }
            }
            if (showBuckets != shouldShowBuckets)
            {
                UpdateBuckets(shouldShowBuckets);
            }

            base.Update(gameTime);
        }

        private void UpdateBuckets(bool show)
        {
            //Console.WriteLine("Update buckets called");
            //float targetY = show ? screenDimensions.Y+20 : screenDimensions.Y+170;

            float targetY = show ? screenDimensions.Y - bucketRed.Height / 2 : screenDimensions.Y;
            targetY = PixelToWorld(new Vector2(0, targetY)).Y;

            bool atRest = true;
            foreach (Bucket b in buckets)
            {
                //b.position.Y += (targetY - b.position.Y)/4;
                b.position.Y = targetY;
                b.physicalBody.Position = b.position;

                atRest &= Math.Abs(b.position.Y - targetY) < 1;
            }
            if (atRest)
            {
                //Console.WriteLine("At rest");
                showBuckets = show;
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.LightBlue);

            spriteBatch.Begin();

            spriteBatch.Draw(skyTexture, new Vector2(0, 0), Color.White);

            // Draw all of the balloons
            Texture2D balloonTexture;
            foreach (ClientBalloon balloon in balloons.Values)
            {
                BalloonType balloonType = balloon.Type;

                switch (balloon.OverlayType)
                {
                    case 1:
                        if (balloonType == BalloonType.Twitter)
                            balloonTexture = balloonTwitterSpots;
                        else if (balloonType == BalloonType.News)
                            balloonTexture = balloonNewsSpots;
                        else if (balloonType == BalloonType.CustomContent)
                            balloonTexture = balloonCustomSpots;
                        else
                            balloonTexture = balloonSpots;
                        break;
                    case 2:
                        if (balloonType == BalloonType.Twitter)
                            balloonTexture = balloonTwitterStripes;
                        else if (balloonType == BalloonType.News)
                            balloonTexture = balloonNewsStripes;
                        else if (balloonType == BalloonType.CustomContent)
                            balloonTexture = balloonCustomStripes;
                        else
                            balloonTexture = balloonStripes;
                        break;
                    default:
                        if (balloonType == BalloonType.Twitter)
                            balloonTexture = balloonTwitterWhite;
                        else if (balloonType == BalloonType.News)
                            balloonTexture = balloonNewsWhite;
                        else if (balloonType == BalloonType.CustomContent)
                            balloonTexture = balloonCustomWhite;
                        else
                            balloonTexture = balloonWhite;
                        break;
                }

                spriteBatch.Draw(balloonTexture, WorldBodyToPixel(balloon.Body.Position, new Vector2(balloonTexture.Width, BalloonHeight)),
                                new Color(balloon.BackgroundColor.Red, balloon.BackgroundColor.Green, balloon.BackgroundColor.Blue, balloon.BackgroundColor.Alpha));
                //balloon image is see-through for some reason, will be fixed for final version. ignore this for now. -lauren
                //TODO: fix balloon image so not so see-through
                spriteBatch.Draw(balloonTexture, WorldBodyToPixel(balloon.Body.Position, new Vector2(balloonTexture.Width, BalloonHeight)),
                                new Color(balloon.BackgroundColor.Red, balloon.BackgroundColor.Green, balloon.BackgroundColor.Blue, balloon.BackgroundColor.Alpha));

                //balloons that are not customizable need boxes for their text
                if (balloonType != BalloonType.Customizable)
                {
                    Vector2 position = WorldToPixel(balloon.Body.Position);
                    position -= new Vector2(boxTexture.Width / 2, boxTexture.Height / 2);
                    position.Y += balloonTexture.Height - (BalloonWidth / 2);
                    Console.WriteLine("Position: " + position);
                    spriteBatch.Draw(boxTexture, position, Color.White);
                    drawSummaryText(balloon.Label, new Vector2(position.X + boxTexture.Width / 20, position.Y + boxTexture.Height * 2 / 3));
                }
            }

            //Draw all buckets
            int bucketIndex = 0;
            Texture2D bucketTexture = bucketRed;
            foreach (Bucket bucket in buckets)
            {
                switch (bucketIndex)
                {
                    case 0:
                        bucketTexture = bucketRed;
                        break;
                    case 1:
                        bucketTexture = bucketSpots;
                        break;
                    case 2:
                        bucketTexture = bucketGreen;
                        break;
                    case 3:
                        bucketTexture = bucketStripes;
                        break;
                    case 4:
                        bucketTexture = bucketBlue;
                        break;
                }
                spriteBatch.Draw(bucketTexture, WorldBodyToPixel(bucket.position, WorldToPixel(bucket.size)), Color.White);
                bucketIndex++;
            }

            //display content page if balloonPopped is true (should only be true for 30 seconds)
            if (poppedBalloonID != null)
            {
                spriteBatch.Draw(contentBox, new Vector2(0, 0), Color.White);
                drawContentText(balloons[poppedBalloonID].Content, new Vector2(screenDimensions.X / 6, screenDimensions.Y / 5));
            }
            else
            {
                // Draw all of the registered hands
                foreach (KeyValuePair<Hand, BodyJointPair> handBody in handBodies)
                {
                    Vector2 cursorPos = WorldBodyToPixel(handBody.Value.Body.Position, new Vector2(handTexture.Width, handTexture.Height));
                    spriteBatch.Draw(handTexture, cursorPos, Color.White);
                }
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

        //TODO: do these properly
        //(have to do new lines manually, based on number of characters in string..?
        private void drawContentText(String text, Vector2 pos)
        {
            spriteBatch.DrawString(textContent, text, pos, Color.Black);
        }

        private void drawSummaryText(String text, Vector2 pos)
        {
            spriteBatch.DrawString(textSummary, text, pos, Color.Black);
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
            objects.Add(handBody, new WorldObject { type = WorldObject.objType.Hand, hand = hand });
        }

        private void RemoveHandFixture(Hand hand)
        {
            BodyJointPair bodyJoint = handBodies[hand];
            _world.RemoveJoint(bodyJoint.Joint);
            _world.RemoveBody(bodyJoint.Body);
            handBodies.Remove(hand);
            objects.Remove(bodyJoint.Body);
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
            //Console.WriteLine("Bucket {0} collided with ballon {1}", bucket.ID, balloon.ID);

            //only change colour if we're touching a new bucket
            //...what if we want to hit the same bucket twice? :S
            if (bucket.ID != oldBucketID)
            {
                int count = 0;
                if (balloon.BackgroundColor.Blue == 0) count++;
                if (balloon.BackgroundColor.Red == 0) count++;
                if (balloon.BackgroundColor.Green == 0) count++;

                if (bucket.ID == 0)
                {
                    //red
                    switch (count)
                    {
                        case 0:
                            balloon.BackgroundColor = new Colour(255, 0, 0, 255);
                            break;
                        case 1:
                            if (balloon.BackgroundColor.Red == 128)
                                balloon.BackgroundColor = new Colour(255, 0, 0, 255);
                            else
                                balloon.BackgroundColor = new Colour(128, 128, 128, 255);
                            break;
                        case 2:
                            if (balloon.BackgroundColor.Red == 0 && balloon.BackgroundColor.Blue == 255)
                                balloon.BackgroundColor = new Colour(128, 0, 128, 255);
                            else if (balloon.BackgroundColor.Red == 0 && balloon.BackgroundColor.Green == 255)
                                balloon.BackgroundColor = new Colour(128, 128, 0, 255);
                            break;
                    }
                }
                else if (bucket.ID == 1)
                {
                    //texture id 1
                    if (balloon.OverlayType == bucket.ID)
                    {
                        balloon.OverlayType = 0;
                    }
                    else
                    {
                        balloon.OverlayType = 1;
                    }
                }
                else if (bucket.ID == 2)
                {
                    //green
                    switch (count)
                    {
                        case 0:
                            balloon.BackgroundColor = new Colour(0, 255, 0, 255);
                            break;
                        case 1:
                            if (balloon.BackgroundColor.Green == 128)
                                balloon.BackgroundColor = new Colour(0, 255, 0, 255);
                            else
                                balloon.BackgroundColor = new Colour(128, 128, 128, 255);
                            break;
                        case 2:
                            if (balloon.BackgroundColor.Green == 0 && balloon.BackgroundColor.Blue == 255)
                                balloon.BackgroundColor = new Colour(0, 128, 128, 255);
                            else if (balloon.BackgroundColor.Green == 0 && balloon.BackgroundColor.Red == 255)
                                balloon.BackgroundColor = new Colour(128, 128, 0, 255);
                            break;
                    }
                }
                else if (bucket.ID == 3)
                {
                    //texture id 2
                    if (balloon.OverlayType == bucket.ID)
                    {
                        balloon.OverlayType = 0;
                    }
                    else
                    {
                        balloon.OverlayType = 2;
                    }
                }
                else if (bucket.ID == 4)
                {
                    //blue
                    switch (count)
                    {
                        case 0:
                            balloon.BackgroundColor = new Colour(0, 0, 255, 255);
                            break;
                        case 1:
                            if (balloon.BackgroundColor.Blue == 128)
                                balloon.BackgroundColor = new Colour(0, 0, 255, 255);
                            else
                                balloon.BackgroundColor = new Colour(128, 128, 128, 255);
                            break;
                        case 2:
                            if (balloon.BackgroundColor.Blue == 0 && balloon.BackgroundColor.Green == 255)
                                balloon.BackgroundColor = new Colour(0, 128, 128, 255);
                            else if (balloon.BackgroundColor.Green == 0 && balloon.BackgroundColor.Red == 255)
                                balloon.BackgroundColor = new Colour(128, 0, 128, 255);
                            break;
                    }
                }
                oldBucketID = bucket.ID;
            }
        }

        private void PopBalloon(string balloonID)
        {
            ClientBalloon balloon = balloons[balloonID];
            if (balloon == null)
            {
                throw new ArgumentOutOfRangeException("e", "No such balloon in received message.");
            }

            // Display content only if balloon is not customizable type
            if (BalloonType.Customizable != balloon.Type)
            {
                poppedBalloonID = balloonID;

                Timer timer = new Timer();
                timer.Elapsed += delegate(Object o, ElapsedEventArgs e)
                {
                    poppedBalloonID = null;
                    timer.Stop();
                };
                timer.Interval = MessageDisplayTime;
                timer.Start();
            }
        }
    }
}
