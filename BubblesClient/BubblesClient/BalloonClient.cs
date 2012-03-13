using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Balloons.Messaging.Model;
using BubblesClient.Input;
using BubblesClient.Model;
using BubblesClient.Model.Buckets;
using BubblesClient.Model.ContentBox;
using BubblesClient.Network;
using BubblesClient.Physics;
using BubblesClient.Utility;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BubblesClient
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BalloonClient : Microsoft.Xna.Framework.Game
    {
        // Textures
        private Texture2D skyTexture, handTexture, planeTexture;

        // Balloon Textures
        private Dictionary<BalloonType, Dictionary<OverlayType, Texture2D>> balloonTextures;
        private Texture2D[] balloonPopTextures;

        private Texture2D boxWhiteColour, boxBlackColour;
        private SpriteFont summaryFont;

        // Network
        public INetworkManager NetworkManager { get; private set; }
        private Dictionary<MessageType, Action<Message>> messageHandlers;

        // XNA Graphics
        private GraphicsDeviceManager graphics;
        private Vector2 screenDimensions;
        private SpriteBatch spriteBatch;

        // Input
        private IInputManager input;
        private Color[] userColours = { Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Purple }; // Move this to config?

        // Physics World
        private Dictionary<string, ClientBalloon> balloons = new Dictionary<string, ClientBalloon>();
        private Dictionary<string, ClientPlane> planes = new Dictionary<string, ClientPlane>();
        private List<Bucket> buckets = new List<Bucket>();
        private PhysicsManager physicsManager = new PhysicsManager();

        private Dictionary<ClientBalloon, WorldEntity> balloonEntities = new Dictionary<ClientBalloon, WorldEntity>();
        private Dictionary<ClientPlane, WorldEntity> planeEntities = new Dictionary<ClientPlane, WorldEntity>();

        private Random rng = new Random();

        //other stuff
        private bool showBuckets = true;
        private Bucket oldBucket = null;

        // If this is not null then we will be showing a balloon. We really need a state machine.
        private AbstractContentBox contentBox;

        private List<PopAnimation> popAnimations = new List<PopAnimation>();
        private GameTime currentTime;

        public BalloonClient(INetworkManager screenManager, IInputManager controller)
        {
            // Initialise Graphics
            graphics = new GraphicsDeviceManager(this);
            if (Configuration.FullScreen)
            {
                // use the current resolution of the screen
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                graphics.IsFullScreen = true;
            }
            else
            {
                if (Configuration.ScreenWidth > 0)
                {
                    graphics.PreferredBackBufferWidth = Configuration.ScreenWidth;
                }
                if (Configuration.ScreenHeight > 0)
                {
                    graphics.PreferredBackBufferHeight = Configuration.ScreenHeight;
                }
            }
            graphics.PreferMultiSampling = true;
            screenDimensions = new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

            // Create a new content box
            if (Configuration.UseHtmlRendering)
            {
                contentBox = new HTMLContentBox(screenDimensions, graphics);
            }
            else
            {
                contentBox = new ManualContentBox(screenDimensions, graphics);
            }

            // Initialise Input
            this.input = controller;
            input.Initialize(screenDimensions);

            // Initialise Content
            Content.RootDirectory = "Content";

            // Initialise network
            this.NetworkManager = screenManager;
            messageHandlers = new Dictionary<MessageType, Action<Message>>();
            messageHandlers[MessageType.NewBalloon] = Wrap<NewBalloonMessage>(OnNewBalloon);
            messageHandlers[MessageType.NewPlane] = Wrap<NewPlaneMessage>(OnNewPlane);
            messageHandlers[MessageType.PopObject] = Wrap<PopObjectMessage>(OnPopObject);
            messageHandlers[MessageType.BalloonContentUpdate] = Wrap<BalloonContentUpdateMessage>(OnBalloonContentUpdate);
            messageHandlers[MessageType.BalloonStateUpdate] = Wrap<BalloonStateUpdateMessage>(OnBalloonStateUpdate);
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

            // Initialise Physics
            physicsManager.Initialize(handTexture.Width);
            physicsManager.BalloonPopped += delegate(object o, PhysicsManager.BalloonPoppedEventArgs args)
            {
                Balloon b = balloonEntities.First(x => x.Value == args.Balloon).Key;
                PopBalloon(b.ID, true);
            };
            physicsManager.BucketCollision += delegate(object o, PhysicsManager.BucketCollisionEventArgs args)
            {
                ClientBalloon balloon = balloonEntities.First(x => x.Value == args.Balloon).Key;
                Bucket bucket = buckets.First(x => x.Entity == args.Bucket);
                this.ApplyBucketToBalloon(bucket, balloon);
            };

            // Create a roof and floor
            physicsManager.CreateBoundary((int)screenDimensions.X * 4, new Vector2(screenDimensions.X / 2, 0));
            physicsManager.CreateBoundary((int)screenDimensions.X * 4, new Vector2(screenDimensions.X / 2, screenDimensions.Y));

            // Load buckets
            float gapBetweenBuckets = (screenDimensions.X - (Bucket.BucketWidth * 5) + 200) / 6.0f;

            for (int i = 0; i < buckets.Count; i++)
            {
                float x = ((i + 1) * gapBetweenBuckets + (i + 0.5f) * Bucket.BucketWidth) - 100;
                float y = screenDimensions.Y - Bucket.BucketHeight;

                Bucket b = buckets[i];
                b.Position = PhysicsManager.PixelToWorld(new Vector2(x, y));
                b.Size = PhysicsManager.PixelToWorld(new Vector2(Bucket.BucketWidth, Bucket.BucketHeight));
                b.Entity = physicsManager.CreateBucket(b.Size, b.Position);
            }

            // Always do this last
            this.NetworkManager.Connect();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            summaryFont = Content.Load<SpriteFont>("Fonts/SpriteFontLarge");

            skyTexture = Content.Load<Texture2D>("Images/Sky");
            handTexture = Content.Load<Texture2D>("Images/Hand");
            planeTexture = Content.Load<Texture2D>("Images/plane-right");

            balloonTextures = new Dictionary<BalloonType, Dictionary<OverlayType, Texture2D>>()
            {
                { BalloonType.Customizable, new Dictionary<OverlayType, Texture2D>() {
                    { OverlayType.White, Content.Load<Texture2D>("Images/BalloonWhite") },
                    { OverlayType.Spots, Content.Load<Texture2D>("Images/BalloonSpots") },
                    { OverlayType.Stripes, Content.Load<Texture2D>("Images/BalloonStripes") }
                } },
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
            };

            balloonPopTextures = new Texture2D[] {
                Content.Load<Texture2D>("Images/BalloonPop")
            };

            boxBlackColour = new Texture2D(GraphicsDevice, 1, 1);
            boxBlackColour.SetData(new[] { Color.Black });
            boxWhiteColour = new Texture2D(GraphicsDevice, 1, 1);
            boxWhiteColour.SetData(new[] { Color.White });

            // Create buckets
            buckets.Add(new ColourBucket(Content.Load<Texture2D>("Images/BucketRed"), Color.Red));
            buckets.Add(new DecorationBucket(Content.Load<Texture2D>("Images/bucketSpots"), OverlayType.Spots));
            buckets.Add(new ColourBucket(Content.Load<Texture2D>("Images/bucketGreen"), Color.Green));
            buckets.Add(new DecorationBucket(Content.Load<Texture2D>("Images/bucketStripes"), OverlayType.Stripes));
            buckets.Add(new ColourBucket(Content.Load<Texture2D>("Images/bucketBlue"), Color.Blue));

            // Set up the content box
            contentBox.LoadResources(Content);

            contentBox.OnClose += delegate(object sender, EventArgs args)
            {
                physicsManager.EnableHandCollisions();
            };
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            base.UnloadContent();

            boxBlackColour.Dispose();
            boxWhiteColour.Dispose();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            currentTime = gameTime;

            // Query the Network Manager for events
            NetworkManager.ProcessMessages(messageHandlers);

            this.HandleInput(gameTime);

            physicsManager.ApplyWind();
            physicsManager.Update(gameTime);

            // Check if any of the balloons have left the screen
            List<ClientBalloon> removals = new List<ClientBalloon>();

            foreach (ClientBalloon balloon in balloons.Values)
            {
                WorldEntity balloonEntity = balloonEntities[balloon];
                Vector2 balloonPosition = balloonEntity.Body.Position;

                if (balloonPosition.X < (ClientBalloon.BalloonWidth * -Configuration.BalloonDeadzoneMultiplier) / PhysicsManager.MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    NetworkManager.MoveBalloonOffscreen(balloon, Direction.Left, exitHeight, balloonEntity.Body.LinearVelocity);
                    removals.Add(balloon);
                }
                else if (balloonPosition.X > (ClientBalloon.BalloonWidth * Configuration.BalloonDeadzoneMultiplier + screenDimensions.X) / PhysicsManager.MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    NetworkManager.MoveBalloonOffscreen(balloon, Direction.Right, exitHeight, balloonEntity.Body.LinearVelocity);
                    removals.Add(balloon);
                }
            }

            removals.ForEach(x => RemoveBalloon(x, false));

            // Move planes and check if any has left the screen
            var planeRemovals = new List<ClientPlane>();
            foreach (ClientPlane plane in planes.Values)
            {
                WorldEntity planeEntity = planeEntities[plane];
                Vector2 delta = plane.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                plane.Position += delta;
                if (planeEntity != null)
                {
                    planeEntity.Body.Position = plane.Position;
                }

                if (plane.Position.X < (ClientPlane.PlaneWidth * -Configuration.BalloonDeadzoneMultiplier) / PhysicsManager.MeterInPixels)
                {
                    float exitHeight = (plane.Position.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    NetworkManager.MovePlaneOffscreen(plane, Direction.Left, exitHeight, plane.Velocity, 0.0f);
                    planeRemovals.Add(plane);
                }
                else if (plane.Position.X > (ClientPlane.PlaneWidth * Configuration.BalloonDeadzoneMultiplier + screenDimensions.X) / PhysicsManager.MeterInPixels)
                {
                    float exitHeight = (plane.Position.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    NetworkManager.MovePlaneOffscreen(plane, Direction.Right, exitHeight, plane.Velocity, 0.0f);
                    planeRemovals.Add(plane);
                }
            }

            planeRemovals.ForEach(x => RemovePlane(x.ID));

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

            // Update the pop animations
            for (int i = popAnimations.Count - 1; i >= 0; i--)
            {
                double elapsedMs = gameTime.TotalGameTime.TotalMilliseconds - popAnimations[i].TimePopped.TotalMilliseconds;
                if (elapsedMs >= Configuration.PopAnimationTime)
                {
                    popAnimations.RemoveAt(i);
                }
                else
                {
                    popAnimations[i].ElapsedSincePopped = (float)(elapsedMs / 1000.0);
                }
            }

            // Update the content box if it is visible
            if (contentBox.IsVisible) { contentBox.Update(gameTime); }

            base.Update(gameTime);
        }

        private void UpdateBuckets(bool show)
        {
            float targetY = (show ? screenDimensions.Y - (Bucket.BucketHeight / 4) : screenDimensions.Y) / PhysicsManager.MeterInPixels;

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

            // Draw all of the boxes first
            foreach (ClientBalloon balloon in balloons.Values)
            {
                // Draw the box containing the balloon text if it is not a user-customized balloon
                if (balloon.ShouldDrawCaption())
                {
                    if (Configuration.UseHtmlRendering)
                    {
                        DrawBalloonCaptionHtml(balloon);
                    }
                    else
                    {
                        DrawBalloonCaptionSprites(balloon);
                    }
                }
            }

            // Draw all of the balloons
            foreach (ClientBalloon balloon in balloons.Values)
            {
                Vector2 balloonPosition = PhysicsManager.WorldBodyToPixel(balloonEntities[balloon].Body.Position, new Vector2(balloon.Texture.Width, ClientBalloon.BalloonHeight));
                Color balloonColour = new Color(balloon.BackgroundColor.Red, balloon.BackgroundColor.Green, balloon.BackgroundColor.Blue, balloon.BackgroundColor.Alpha);
                spriteBatch.Draw(balloon.Texture, balloonPosition, balloonColour);
            }

            // Draw all of the planes
            foreach (ClientPlane plane in planes.Values)
            {
                WorldEntity planeEntity = planeEntities[plane];
                Vector2 planeSize = new Vector2(planeTexture.Width, ClientPlane.PlaneHeight);
                Vector2 planePosition = PhysicsManager.WorldBodyToPixel(plane.Position, planeSize);
                Color planeColour = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                SpriteEffects effects = (plane.Direction == Direction.Right) ?
                    SpriteEffects.None : SpriteEffects.FlipHorizontally;
                spriteBatch.Draw(planeTexture, planePosition, null, planeColour, 0.0f, Vector2.Zero,
                    1.0f,  effects, 0.0f);
            }

            // Draw all pop animations
            foreach (PopAnimation popAnim in popAnimations)
            {
                Rectangle textureRect = new Rectangle((int)popAnim.Pos.X, (int)popAnim.Pos.Y,
                                                popAnim.PopTexture.Width, popAnim.PopTexture.Height);
                Color popColour = new Color(popAnim.PopColour.Red, popAnim.PopColour.Green, popAnim.PopColour.Blue, popAnim.PopColour.Alpha);
                if (Configuration.PopAnimationEnabled)
                {
                    // Animate popped balloons: make the texture bigger/smaller over time through scale
                    float alpha = Configuration.PopAnimationAlpha, beta = Configuration.PopAnimationBeta;
                    float balloonScale = 1.0f + alpha * (float)Math.Sin(beta * popAnim.ElapsedSincePopped);
                    balloonScale *= Configuration.PopAnimationScale;

                    // Scale the texture rectangle at its center and not at its top-left corner
                    // like Draw() does when you pass a scaling factor.
                    float newWidth = (textureRect.Width * balloonScale);
                    float newHeight = (textureRect.Height * balloonScale);
                    float newX = (textureRect.Center.X - newWidth * 0.5f);
                    float newY = (textureRect.Center.Y - newHeight * 0.5f);
                    textureRect = new Rectangle((int)newX, (int)newY, (int)newWidth, (int)newHeight);
                }
                spriteBatch.Draw(popAnim.PopTexture, textureRect, popColour);
            }

            // Draw all buckets
            foreach (Bucket bucket in buckets)
            {
                spriteBatch.Draw(bucket.Texture, PhysicsManager.WorldBodyToPixel(bucket.Entity.Body.Position, PhysicsManager.WorldToPixel(bucket.Size)), Color.White);
            }

            // Display content page if the contentBox has a balloon
            if (contentBox.IsVisible)
            {
                contentBox.Draw(spriteBatch);
            }

            // Draw all of the registered hands
            foreach (WorldEntity handBody in physicsManager.GetHandPositions())
            {
                Vector2 cursorPos = PhysicsManager.WorldBodyToPixel(handBody.Body.Position, new Vector2(handTexture.Width, handTexture.Height));
                Hand hand = physicsManager.GetHandForHandEntity(handBody);
                Color col = userColours[hand.ID % 2];
                SpriteEffects eff = hand.Side == Side.Left ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                spriteBatch.Draw(handTexture, cursorPos, null, col, 0, Vector2.Zero, 1, eff, 0);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawBalloonCaptionSprites(ClientBalloon balloon)
        {
            // If the label is not cached then it means it has not
            // been formatted to fit in the box; therefore format it 
            // and save it back
            if (!balloon.IsLabelCached)
            {
                string labelText = balloon.Label;
                balloon.Label = TextUtility.wrapText(summaryFont, labelText, new Vector2(382, 168));
                balloon.IsLabelCached = true;
            }

            // Measure the size of the string and add 4px padding
            Vector2 boxSize = summaryFont.MeasureString(balloon.Label) + new Vector2(8, 8);

            Vector2 boxPosition =
                PhysicsManager.WorldToPixel(balloonEntities[balloon].Body.Position)
                - new Vector2(boxSize.X / 2, 0);
            boxPosition.Y += balloon.Texture.Height - (ClientBalloon.BalloonHeight / 2);

            spriteBatch.Draw(boxBlackColour, new Rectangle((int)boxPosition.X - 2, (int)boxPosition.Y - 2, (int)boxSize.X + 4, (int)boxSize.Y + 4), Color.White);
            spriteBatch.Draw(boxWhiteColour, new Rectangle((int)boxPosition.X, (int)boxPosition.Y, (int)boxSize.X, (int)boxSize.Y), Color.White);

            TextUtility.drawTextLabel(spriteBatch, summaryFont, balloon.Label, new Vector2(boxPosition.X, boxPosition.Y) + new Vector2(4, 4));
        }

        private void DrawBalloonCaptionHtml(ClientBalloon balloon)
        {
            Vector2 boxPosition =
                PhysicsManager.WorldToPixel(balloonEntities[balloon].Body.Position);
            boxPosition.Y += balloon.Texture.Height - (ClientBalloon.BalloonHeight / 2);

            Texture2D caption = balloon.BalloonContentCache[CacheType.Caption, GraphicsDevice];
            if (caption != null)
            {
                boxPosition.X -= caption.Width / 2;
                spriteBatch.Draw(caption, boxPosition, Color.White);
            }
        }

        private void HandleInput(GameTime gameTime)
        {
            Hand[] hands = input.GetHandPositions();
            physicsManager.UpdateHandPositions(hands);

            // If the content box is visible, check if the hands are in the correct position
            if (contentBox.IsVisible)
            {
                bool insideThisFrame = false;
                foreach (Hand hand in hands)
                {
                    // If hand's position is inside the boxes then fire the event
                    if (((hand.Position.X >= screenDimensions.X - 136 && hand.Position.X <= screenDimensions.X) &&
                        (hand.Position.Y >= 0 && hand.Position.Y <= 136)) ||
                        ((hand.Position.X >= 0 && hand.Position.X < 136) && 
                        (hand.Position.Y >= 0 && hand.Position.Y <= 136)))
                    {
                        insideThisFrame = true;
                        break;
                    }
                }

                if (insideThisFrame)
                {
                    contentBox.CountDownCloseTimer(gameTime);
                }
                else
                {
                    contentBox.CancelCloseTimer();
                }
            }
        }

        private void ApplyBucketToBalloon(Bucket bucket, ClientBalloon balloon)
        {
            OverlayType oldOverlay = balloon.OverlayType;
            Colour oldBackgroundColor = balloon.BackgroundColor;

            // Only change colour if we're touching a new bucket
            if (bucket != oldBucket)
            {
                bucket.ApplyToBalloon(balloon);

                oldBucket = bucket;
            }

            // notify the server that the ballon's decoration changed.
            // otherwise decorations are lost when balloons change screens
            if (balloon.OverlayType != oldOverlay || !balloon.BackgroundColor.Equals(oldBackgroundColor))
            {
                NetworkManager.UpdateBalloonDetails(balloon);
            }

            balloon.Texture = balloonTextures[balloon.Type][balloon.OverlayType];
        }

        #region "Balloon Popping"
        private void PopBalloon(string balloonID, bool showContent = false)
        {
            ClientBalloon balloon = balloons[balloonID];
            if (balloon == null)
            {
                throw new ArgumentOutOfRangeException("e", "No such balloon in received message.");
            }

            // Display content only asked and if balloon has a caption
            showContent &= balloon.ShouldDrawCaption();
            balloon.Popped = true;
            if (showContent)
            {
                contentBox.SetBalloon(balloon);
                physicsManager.DisableHandCollisions();
            }

            RemoveBalloon(balloon);
        }

        /// <summary>
        /// Removes (immediately) the given balloon from the physics world and screen.
        /// </summary>
        /// <param name="balloon">Balloon to remove. </param>
        private void RemoveBalloon(ClientBalloon balloon, Boolean animate = true)
        {
            if (animate)
            {
                // Create a new pop animation
                PopAnimation anim = new PopAnimation(balloon);
                anim.Pos = PhysicsManager.WorldToPixel(balloonEntities[balloon].Body.Position);
                anim.TimePopped = currentTime.TotalGameTime;
                anim.PopTexture = balloonPopTextures[rng.Next(0, balloonPopTextures.Length)];
                anim.PopColour = new Colour(255, 255, 255, 255);
                popAnimations.Add(anim);
            }

            // Remove balloon from screen.
            physicsManager.RemoveEntity(balloonEntities[balloon]);
            balloonEntities.Remove(balloon);
            balloons.Remove(balloon.ID);

            // Remove balloon from server. This must be done after removing the balloon
            // from the map or an exception can be raised when the server sends a
            // "new balloon" message with the same ID when the feed is updated.
            NetworkManager.NotifyBalloonPopped(balloon);
        }

        private void RemovePlane(string planeID)
        {
            ClientPlane plane = planes[planeID];
            if (plane == null)
            {
                throw new ArgumentOutOfRangeException("e", "No such plane in received message.");
            }
            WorldEntity planeEntity = planeEntities[plane];
            if (planeEntity != null)
            {
                physicsManager.RemoveEntity(planeEntity);
            }
            planeEntities.Remove(plane);
            planes.Remove(plane.ID);
        }
        #endregion

        #region "Networking"
        public void OnNewBalloon(NewBalloonMessage m)
        {
            // Choose where to place the balloon
            Vector2 position = GetInitialPosition(m.Direction, m.Y, ClientBalloon.BalloonWidth);

            // Setup the balloon's body
            Vector2 velocity = new Vector2(m.Velocity.X, m.Velocity.Y);
            WorldEntity balloonEntity = physicsManager.CreateBalloon(position, velocity);

            Balloon balloon = NetworkManager.GetBalloonDetails(m.ObjectID);
            ClientBalloon b = new ClientBalloon(balloon);
            b.Texture = balloonTextures[balloon.Type][balloon.OverlayType];
            b.BalloonContentCache = contentBox.GetBalloonContent(b.ID);

            // Render the balloon's caption if we already have it
            if (!String.IsNullOrWhiteSpace(b.Label))
            {
                contentBox.GenerateCaption(b);
            }

            balloons.Add(b.ID, b);
            balloonEntities[b] = balloonEntity;
        }

        public void OnNewPlane(NewPlaneMessage m)
        {
            if (planes.ContainsKey(m.ObjectID))
            {
                return;
            }

            // Choose where to place the plane
            Direction planeDirection;
            Vector2D velocity;
            const float velocityMod = 0.5f;
            switch (m.Direction)
            {
            default:
            case Direction.Any:
                throw new Exception("Invalid direction for new plane");
            case Direction.Left:
                // The plane appears on the left side of the screen, it is going right
                planeDirection = Direction.Right;
                velocity = Configuration.VelocityRight;
                break;
            case Direction.Right:
                // The plane appears on the right side of the screen, it is going left
                planeDirection = Direction.Left;
                velocity = Configuration.VelocityLeft;
                break;
            }

            Vector2 pixPosition = GetInitialPosition(m.Direction, m.Y, ClientPlane.PlaneWidth);

            ClientPlane plane = new ClientPlane(m.ObjectID, m.PlaneType);
            plane.Velocity = new Vector2(velocity.X, velocity.Y) * velocityMod;
            plane.Position = PhysicsManager.PixelToWorld(pixPosition);
            plane.Direction = planeDirection;
            planes.Add(plane.ID, plane);

            // Setup the plane's body
            WorldEntity planeEntity = physicsManager.CreatePlane(plane.Position);
            planeEntities[plane] = planeEntity;
        }

        private Vector2 GetInitialPosition(Direction dir, float y, float objWidth)
        {
            Vector2 position = new Vector2();
            switch (dir)
            {
            case Direction.Left:
                position.X = objWidth * -1;
                break;
            case Direction.Right:
                position.X = objWidth + screenDimensions.X;
                break;

            case Direction.Any:
            default:
                position.X = rng.Next((int)screenDimensions.X);
                break;
            }

            position.Y = y * screenDimensions.Y;
            return position;
        }

        /// <summary>
        /// Handles the case where the server forces us to pop an object (ballon or plane).
        /// </summary>
        public void OnPopObject(PopObjectMessage m)
        {
            if (balloons.ContainsKey(m.ObjectID))
            {
                PopBalloon(m.ObjectID);
            }

            if (planes.ContainsKey(m.ObjectID))
            {
                RemovePlane(m.ObjectID);
            }
        }

        public void OnBalloonContentUpdate(BalloonContentUpdateMessage bcm)
        {
            ClientBalloon balloon;
            if (balloons.TryGetValue(bcm.ObjectID, out balloon))
            {
                string oldLabel = balloon.Label;
                balloon.Label = bcm.Label;
                balloon.Content = bcm.Content;
                balloon.Type = bcm.BalloonType;
                balloon.Url = bcm.Url;
                balloon.ImageUrl = bcm.ImageUrl;

                // Generate the balloon's caption again when it changes
                if (oldLabel != balloon.Label)
                {
                    contentBox.GenerateCaption(balloon);
                }
            }
        }

        public void OnBalloonStateUpdate(BalloonStateUpdateMessage bdm)
        {
            ClientBalloon balloon;
            if (balloons.TryGetValue(bdm.ObjectID, out balloon))
            {
                int oldVotes = balloon.Votes;
                balloon.OverlayType = bdm.OverlayType;
                balloon.BackgroundColor = bdm.BackgroundColor;
                balloon.Votes = bdm.Votes;
                // Generate the balloon's content again when the number of votes changes
                if (oldVotes != balloon.Votes)
                {
                    contentBox.GenerateTextContent(balloon);
                }
            }
        }

        /// <summary>
        /// Wrap a type-specific message handler to a generic handler.
        /// </summary>
        /// <typeparam name="T"> Type of the message to handle. </typeparam>
        /// <param name="handler"> Handler delegate to wrap. </param>
        /// <returns> Generic message handler. </returns>
        private static Action<Message> Wrap<T>(Action<T> handler) where T : Message
        {
            return delegate(Message msg)
            {
                handler((T)msg);
            };
        }
        #endregion
    }
}
