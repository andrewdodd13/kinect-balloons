using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Balloons.Messaging.Model;
using BubblesClient.Input.Controllers;
using BubblesClient.Model;
using BubblesClient.Physics;
using FarseerPhysics.Dynamics;
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
        // Textures
        private Texture2D skyTexture, handTexture, contentBox;

        // Balloon Textures
        private Dictionary<BalloonType, Dictionary<OverlayType, Texture2D>> balloonTextures;
        private Texture2D[] bucketTextures = new Texture2D[5];

        private Texture2D boxTexture;
        private SpriteFont textContent, textSummary;

        // Network
        public ScreenManager ScreenManager { get; private set; }

        // XNA Graphics
        private GraphicsDeviceManager graphics;
        private Vector2 screenDimensions;
        private SpriteBatch spriteBatch;

        // Input
        private IInputController input;

        // Physics World
        private Dictionary<string, ClientBalloon> balloons = new Dictionary<string, ClientBalloon>();
        private List<Bucket> buckets = new List<Bucket>();
        private PhysicsManager physicsManager = new PhysicsManager();

        private Dictionary<ClientBalloon, WorldEntity> balloonEntities = new Dictionary<ClientBalloon, WorldEntity>();

        //other stuff
        private bool showBuckets = true;
        private int oldBucketID = 5; //buckets 0-4

        // The time to display a message for, in milliseconds
        private const int MessageDisplayTime = 30000;

        // If this is not null then we will be showing a balloon. We really need
        // a state machine.
        private string poppedBalloonID = null;

        public BubblesClientGame(ScreenManager screenManager, IInputController controller)
        {
            // Initialise Graphics
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 768;
            graphics.PreferredBackBufferWidth = 1366;

            screenDimensions = new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

            // Initialise Input
            this.input = controller;
            input.Initialize(screenDimensions);

            // Initialise Content
            Content.RootDirectory = "Content";

            // Initialise Physics
            physicsManager.Initialize();
            physicsManager.BalloonPopped += delegate(object o, PhysicsManager.BalloonPoppedEventArgs args)
            {
                Balloon b = balloonEntities.First(x => x.Value == args.Balloon).Key;
                PopBalloon(b.ID);
            };
            physicsManager.BucketCollision += delegate(object o, PhysicsManager.BucketCollisionEventArgs args)
            {
                ClientBalloon balloon = balloonEntities.First(x => x.Value == args.Balloon).Key;
                Bucket bucket = buckets.First(x => x.Entity == args.Bucket);
                ApplyBucketToBalloon(bucket, balloon);
            };

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
                    case MessageType.BalloonContentUpdate:
                        OnBalloonContentUpdate((BalloonContentUpdateMessage)msg);
                        break;
                    case MessageType.BalloonDecorationUpdate:
                        OnBalloonDecorationUpdate((BalloonDecorationUpdateMessage)msg);
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
                    position.X = ClientBalloon.BalloonWidth * -1;
                    break;
                case Direction.Right:
                    position.X = ClientBalloon.BalloonWidth + screenDimensions.X;
                    break;

                case Direction.Any:
                default:
                    position.X = new Random().Next((int)screenDimensions.X);
                    break;
            }

            position.Y = m.Y * screenDimensions.Y;

            // Setup the balloon's body
            Vector2 velocity = new Vector2(m.Velocity.X, m.Velocity.Y);
            WorldEntity balloonEntity = physicsManager.CreateBalloon(position, velocity);

            Balloon balloon = ScreenManager.GetBalloonDetails(m.BalloonID);
            ClientBalloon b = new ClientBalloon(balloon);

            balloons.Add(b.ID, b);
            balloonEntities.Add(b, balloonEntity);
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

        public void OnBalloonContentUpdate(BalloonContentUpdateMessage bcm)
        {
            ClientBalloon balloon;
            if (balloons.TryGetValue(bcm.BalloonID, out balloon))
            {
                balloon.Label = bcm.Label;
                balloon.Content = bcm.Content;
                balloon.Type = bcm.BalloonType;
                balloon.Url = bcm.Url;
            }
        }

        public void OnBalloonDecorationUpdate(BalloonDecorationUpdateMessage bdm)
        {
            ClientBalloon balloon;
            if (balloons.TryGetValue(bdm.BalloonID, out balloon))
            {
                balloon.OverlayType = bdm.OverlayType;
                balloon.BackgroundColor = bdm.BackgroundColor;
            }
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
            physicsManager.CreateRoof((int)screenDimensions.X * 4, new Vector2(screenDimensions.X / 2, 0));

            //Load buckets
            //Note to self: Prettify - William
            // TODO: Should this be the first line... or the second? :P
            float gapBetweenBuckets = (screenDimensions.Y - (Bucket.BucketWidth * 5)) / 6;
            gapBetweenBuckets = 121;
            for (int i = 0; i < 5; i++)
            {
                float x = (i + 1) * gapBetweenBuckets + (i + 0.5f) * Bucket.BucketWidth;
                float y = screenDimensions.Y - Bucket.BucketHeight;

                Bucket b = new Bucket()
                {
                    ID = i,
                    Position = PhysicsManager.PixelToWorld(new Vector2(x, y)),
                    Size = PhysicsManager.PixelToWorld(new Vector2(Bucket.BucketWidth, Bucket.BucketHeight)),
                    Texture = bucketTextures[i]
                };
                buckets.Add(b);

                b.Entity = physicsManager.CreateBucket(b.Size, b.Position);
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

            balloonTextures = new Dictionary<BalloonType, Dictionary<OverlayType, Texture2D>>()
            {
                { BalloonType.CustomContent, new Dictionary<OverlayType, Texture2D>() {
                    { OverlayType.White, Content.Load<Texture2D>("Images/BalloonWhiteCustom") },
                    { OverlayType.Spots, Content.Load<Texture2D>("Images/BalloonSpotsCustom")},
                    { OverlayType.Stripes, Content.Load<Texture2D>("Images/BalloonStripesCustom") }
                } },
                { BalloonType.Twitter, new Dictionary<OverlayType, Texture2D>() { 
                    { OverlayType.White, Content.Load<Texture2D>("Images/BalloonWhiteTwitter") },
                    { OverlayType.Spots, Content.Load<Texture2D>("Images/BalloonSpotsTwitter") },
                    { OverlayType.Stripes, Content.Load<Texture2D>("Images/BalloonStripesTwitter") }
                } },
                { BalloonType.News, new Dictionary<OverlayType, Texture2D>() { 
                    { OverlayType.White, Content.Load<Texture2D>("Images/BalloonWhiteNews") },
                    { OverlayType.Spots, Content.Load<Texture2D>("Images/BalloonSpotsNews") }, 
                    { OverlayType.Stripes, Content.Load<Texture2D>("Images/BalloonStripesNews") }
                } },
                { BalloonType.Customizable, new Dictionary<OverlayType, Texture2D>() {
                    { OverlayType.White, Content.Load<Texture2D>("Images/BalloonWhite") },
                    { OverlayType.Spots, Content.Load<Texture2D>("Images/BalloonSpots") },
                    { OverlayType.Stripes, Content.Load<Texture2D>("Images/BalloonStripes") }
                } }
            };

            boxTexture = Content.Load<Texture2D>("Images/Box");
            bucketTextures[0] = Content.Load<Texture2D>("Images/BucketRed");
            bucketTextures[2] = Content.Load<Texture2D>("Images/bucketGreen");
            bucketTextures[4] = Content.Load<Texture2D>("Images/bucketBlue");
            bucketTextures[3] = Content.Load<Texture2D>("Images/bucketStripes");
            bucketTextures[1] = Content.Load<Texture2D>("Images/bucketSpots");
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

            physicsManager.Update(gameTime);

            // God this is hacky!
            MouseState mouseState = Mouse.GetState();
            if (mouseState.MiddleButton == ButtonState.Pressed)
            {
                poppedBalloonID = null;
            }

            // Check if any of the balloons have left the screen
            List<ClientBalloon> removals = new List<ClientBalloon>();

            foreach (ClientBalloon balloon in balloons.Values)
            {
                WorldEntity balloonEntity = balloonEntities[balloon];
                Vector2 balloonPosition = balloonEntity.Body.Position;

                // 1.5 width for that extra bit of margin
                if (balloonPosition.X < (ClientBalloon.BalloonWidth * -1.5) / PhysicsManager.MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    ScreenManager.MoveBalloonOffscreen(balloon, Direction.Left, exitHeight, balloonEntity.Body.LinearVelocity);
                    removals.Add(balloon);
                }
                else if (balloonPosition.X > (ClientBalloon.BalloonWidth * 1.5 + screenDimensions.X) / PhysicsManager.MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    ScreenManager.MoveBalloonOffscreen(balloon, Direction.Right, exitHeight, balloonEntity.Body.LinearVelocity);
                    removals.Add(balloon);
                }
            }

            removals.ForEach(x =>
            {
                balloons.Remove(x.ID);
                physicsManager.RemoveEntity(balloonEntities[x]);
            });

            //Show buckets if a balloon is in lower 1/3 of screen
            //I don't like how this is implemented - animation speed is dependant on frame rate
            //Would be better to have a spring or joint moving the buckets, but I haven't quite figured out how they work yet
            bool shouldShowBuckets = false;
            Vector2 screen = PhysicsManager.PixelToWorld(screenDimensions);
            foreach (ClientBalloon balloon in balloons.Values)
            {
                Body balloonBody = balloonEntities[balloon].Body;
                if (balloonBody.Position.Y + (128f / PhysicsManager.MeterInPixels) > screen.Y * 2 / 3)
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
            float targetY = (show ? screenDimensions.Y - Bucket.BucketHeight / 2 : screenDimensions.Y) / PhysicsManager.MeterInPixels;

            bool atRest = true;
            foreach (Bucket b in buckets)
            {
                b.Position = new Vector2(b.Position.X, targetY);
                b.Entity.Body.Position = b.Position;

                atRest &= Math.Abs(b.Position.Y - targetY) < 1;
            }
            if (atRest)
            {
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
            foreach (ClientBalloon balloon in balloons.Values)
            {
                Texture2D balloonTexture = balloonTextures[balloon.Type][balloon.OverlayType];
                Vector2 balloonPosition = PhysicsManager.WorldBodyToPixel(balloonEntities[balloon].Body.Position, new Vector2(balloonTexture.Width, ClientBalloon.BalloonHeight));
                spriteBatch.Draw(balloonTexture, balloonPosition, new Color(balloon.BackgroundColor.Red, balloon.BackgroundColor.Green, balloon.BackgroundColor.Blue, balloon.BackgroundColor.Alpha));

                // Draw the box containing the balloon text if it is not a user-customized balloon
                if (balloon.Type != BalloonType.Customizable)
                {
                    Vector2 boxPosition = PhysicsManager.WorldToPixel(balloonEntities[balloon].Body.Position) - new Vector2(boxTexture.Width / 2, boxTexture.Height / 2);
                    boxPosition.Y += balloonTexture.Height - (ClientBalloon.BalloonWidth / 2);

                    spriteBatch.Draw(boxTexture, boxPosition, Color.White);
                    drawSummaryText(balloon.Label, new Vector2(boxPosition.X + boxTexture.Width / 20, boxPosition.Y + boxTexture.Height * 2 / 3));
                }
            }

            // Draw all buckets
            foreach (Bucket bucket in buckets)
            {
                spriteBatch.Draw(bucket.Texture, PhysicsManager.WorldBodyToPixel(bucket.Entity.Body.Position, PhysicsManager.WorldToPixel(bucket.Size)), Color.White);
            }

            //display content page if balloonPopped is true (should only be true for 30 seconds)
            if (poppedBalloonID != null)
            {
                Vector2 position = (screenDimensions / 2) - (new Vector2(contentBox.Width, contentBox.Height) / 2);
                spriteBatch.Draw(contentBox, position, Color.White);
                drawContentText(balloons[poppedBalloonID].Content, new Vector2(screenDimensions.X / 6, screenDimensions.Y / 5));
            }
            else
            {
                // Draw all of the registered hands
                foreach (WorldEntity handBody in physicsManager.GetHandPositions())
                {
                    Vector2 cursorPos = PhysicsManager.WorldBodyToPixel(handBody.Body.Position, new Vector2(handTexture.Width, handTexture.Height));
                    spriteBatch.Draw(handTexture, cursorPos, Color.White);
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void HandleInput()
        {
            physicsManager.UpdateHandPositions(input.GetHandPositions());
        }

        //TODO: do these properly
        //(have to do new lines manually, based on number of characters in string..?
        private void drawContentText(String text, Vector2 pos)
        {
            try
            {
                spriteBatch.DrawString(textContent, text, pos, Color.Black);
            }
            catch (Exception)
            {
                spriteBatch.DrawString(textContent, "Invalid character", pos, Color.Red);
            }
        }

        private void drawSummaryText(String text, Vector2 pos)
        {
            try
            {
                spriteBatch.DrawString(textSummary, text, pos, Color.Black);
            }
            catch (Exception)
            {
                spriteBatch.DrawString(textSummary, "Invalid character", pos, Color.Red);
            }
        }

        private void ApplyBucketToBalloon(Bucket bucket, ClientBalloon balloon)
        {
            //Console.WriteLine("Bucket {0} collided with ballon {1}", bucket.ID, balloon.ID);
            OverlayType oldOverlay = balloon.OverlayType;
            Colour oldBackgroundColor = balloon.BackgroundColor;

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
                    if (balloon.OverlayType == OverlayType.Spots)
                    {
                        balloon.OverlayType = OverlayType.White;
                    }
                    else
                    {
                        balloon.OverlayType = OverlayType.Spots;
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
                    if (balloon.OverlayType == OverlayType.Stripes)
                    {
                        balloon.OverlayType = OverlayType.White;
                    }
                    else
                    {
                        balloon.OverlayType = OverlayType.Stripes;
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

            // notify the server that the ballon's decoration changed.
            // otherwise decorations are lost when ballons change screens
            if (balloon.OverlayType != oldOverlay || !balloon.BackgroundColor.Equals(oldBackgroundColor))
            {
                ScreenManager.UpdateBalloonDetails(balloon);
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
