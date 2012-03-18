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
        private List<Texture2D> confettiTextures;

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

        private Random rng = new Random();

        //other stuff
        private bool showBuckets = true;
        private Bucket oldBucket = null;

        // If this is not null then we will be showing a balloon. We really need a state machine.
        private AbstractContentBox contentBox;
        private Dictionary<string, Balloon> balloonCache;

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
            balloonCache = new Dictionary<string, Balloon>();

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
                string balloonID = balloons.First(x => x.Value.Entity == args.Balloon).Key;
                PopBalloon(balloonID, true);
            };
            physicsManager.BucketCollision += delegate(object o, PhysicsManager.BucketCollisionEventArgs args)
            {
                ClientBalloon balloon = balloons.First(x => x.Value.Entity == args.Balloon).Value;
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

            confettiTextures = new List<Texture2D>();
            for (int i = 1; i <= 60; i++)
            {
                confettiTextures.Add(Content.Load<Texture2D>(String.Format("Images/Confetti/{0:d04}", i)));
            }

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

        #region "Updating"
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
                Vector2 balloonPosition = balloon.Entity.Body.Position;

                if (balloonPosition.X < (ClientBalloon.BalloonWidth * -Configuration.BalloonDeadzoneMultiplier) / PhysicsManager.MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    NetworkManager.MoveBalloonOffscreen(balloon, Direction.Left, exitHeight, balloon.Entity.Body.LinearVelocity);
                    removals.Add(balloon);
                }
                else if (balloonPosition.X > (ClientBalloon.BalloonWidth * Configuration.BalloonDeadzoneMultiplier + screenDimensions.X) / PhysicsManager.MeterInPixels)
                {
                    float exitHeight = (balloonPosition.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    NetworkManager.MoveBalloonOffscreen(balloon, Direction.Right, exitHeight, balloon.Entity.Body.LinearVelocity);
                    removals.Add(balloon);
                }
            }

            removals.ForEach(x => RemoveBalloon(x, false, false));

            var planeRemovals = new List<ClientPlane>();
            foreach (ClientPlane plane in planes.Values)
            {
                // Move the plane
                const float amplitudeY = 2.0f;
                float deltaT = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float deltaX = plane.Velocity.X * deltaT;
                float deltaY = (float)(Math.Cos(plane.Time * 3.5) * Math.Cos(plane.Time) * Math.Cos(plane.Time));
                Vector2 newPos = plane.Position;
                plane.Time += deltaT;
                newPos.X += deltaX;
                newPos.Y = ((plane.InitialY * screenDimensions.Y) / PhysicsManager.MeterInPixels) + (deltaY * amplitudeY);
                plane.Position = newPos;
                if (plane.Entity != null)
                {
                    plane.Entity.Body.Position = plane.Position;
                }

                // Check if the plane has left the screen
                Direction exitDir = Direction.Any;
                Vector2 planeSize = ClientPlane.PlaneSize * ClientPlane.PlaneScale;
                if (plane.Position.X < (planeSize.X * -Configuration.BalloonDeadzoneMultiplier) / PhysicsManager.MeterInPixels)
                {
                    exitDir = Direction.Left;
                }
                else if (plane.Position.X > (planeSize.X * Configuration.BalloonDeadzoneMultiplier + screenDimensions.X) / PhysicsManager.MeterInPixels)
                {
                    exitDir = Direction.Right;
                }
                if (exitDir != Direction.Any)
                {
                    float exitHeight = (plane.Position.Y * PhysicsManager.MeterInPixels) / screenDimensions.Y;
                    NetworkManager.MovePlaneOffscreen(plane, exitDir, exitHeight, plane.Velocity, (float)plane.Time);
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
                Body balloonBody = balloon.Entity.Body;
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
                UpdateBalloonDetails(balloon);
            }

            balloon.Texture = balloonTextures[balloon.Type][balloon.OverlayType];
        }
        #endregion

        #region "Drawing"
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
                DrawBalloonCaption(gameTime, balloon);
            }

            // Draw all of the balloons
            foreach (ClientBalloon balloon in balloons.Values)
            {
                DrawBalloon(gameTime, balloon);
            }

            // Draw all of the planes
            foreach (ClientPlane plane in planes.Values)
            {
                DrawPlane(gameTime, plane);
            }

            // Draw all pop animations
            foreach (PopAnimation popAnim in popAnimations)
            {
                DrawPopAnimation(gameTime, popAnim);
            }

            // Draw all buckets
            foreach (Bucket bucket in buckets)
            {
                DrawBucket(gameTime, bucket);
            }

            // Display content page if the contentBox has a balloon
            if (contentBox.IsVisible)
            {
                contentBox.Draw(spriteBatch);
            }

            // Draw all confetti animations (on top of the content)
            foreach (PopAnimation popAnim in popAnimations)
            {
                DrawConfettiAnimation(gameTime, popAnim);
            }

            // Draw all of the registered hands
            foreach (WorldEntity handBody in physicsManager.GetHandPositions())
            {
                DrawHand(gameTime, handBody);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawBalloon(GameTime gameTime, ClientBalloon balloon)
        {
            Vector2 balloonPosition = PhysicsManager.WorldBodyToPixel(balloon.Entity.Body.Position, new Vector2(balloon.Texture.Width, ClientBalloon.BalloonHeight));
            Color balloonColour = new Color(balloon.BackgroundColor.Red, balloon.BackgroundColor.Green, balloon.BackgroundColor.Blue, balloon.BackgroundColor.Alpha);
            spriteBatch.Draw(balloon.Texture, balloonPosition, balloonColour);
        }

        private void DrawBalloonCaption(GameTime gameTime, ClientBalloon balloon)
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
                PhysicsManager.WorldToPixel(balloon.Entity.Body.Position)
                - new Vector2(boxSize.X / 2, 0);
            boxPosition.Y += balloon.Texture.Height - (ClientBalloon.BalloonHeight / 2);

            spriteBatch.Draw(boxBlackColour, new Rectangle((int)boxPosition.X - 2, (int)boxPosition.Y - 2, (int)boxSize.X + 4, (int)boxSize.Y + 4), Color.White);
            spriteBatch.Draw(boxWhiteColour, new Rectangle((int)boxPosition.X, (int)boxPosition.Y, (int)boxSize.X, (int)boxSize.Y), Color.White);

            TextUtility.drawTextLabel(spriteBatch, summaryFont, balloon.Label, new Vector2(boxPosition.X, boxPosition.Y) + new Vector2(4, 4));
        }

        private void DrawBalloonCaptionHtml(ClientBalloon balloon)
        {
            Vector2 boxPosition =
                PhysicsManager.WorldToPixel(balloon.Entity.Body.Position);
            boxPosition.Y += balloon.Texture.Height - (ClientBalloon.BalloonHeight / 2);

            Texture2D caption = balloon.BalloonContentCache[CacheType.Caption, GraphicsDevice];
            if (caption != null)
            {
                boxPosition.X -= caption.Width / 2;
                spriteBatch.Draw(caption, boxPosition, Color.White);
            }
        }

        private void DrawPlane(GameTime gameTime, ClientPlane plane)
        {
            Vector2 planeSize = ClientPlane.PlaneSize * ClientPlane.PlaneScale;
            Vector2 planePosition = PhysicsManager.WorldBodyToPixel(plane.Position, planeSize);
            SpriteEffects effects = (plane.Direction == Direction.Right) ?
                SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(planeTexture, planePosition, null, Color.White, 0.0f, Vector2.Zero,
                ClientPlane.PlaneScale, effects, 0.0f);

            if (plane.CaptionTexture == null && plane.Caption != null)
            {
                plane.CaptionTexture = ImageGenerator.BitmapToTexture(plane.Caption, GraphicsDevice);
            }

            if (plane.CaptionTexture != null)
            {
                Vector2 captionPosition = planePosition;
                const int margin = 20;
                if (plane.Direction == Direction.Left)
                {
                    captionPosition.X += (planeSize.X + margin);
                }
                else
                {
                    captionPosition.X -= (plane.CaptionTexture.Width + margin);
                }
                captionPosition.Y += (planeSize.Y - plane.CaptionTexture.Height) * 0.5f;
                spriteBatch.Draw(plane.CaptionTexture, captionPosition, Color.White);
            }
        }

        private void DrawPopAnimation(GameTime gameTime, PopAnimation popAnim)
        {
            Vector2 popSize = new Vector2(popAnim.PopTexture.Width, popAnim.PopTexture.Height);
            
            // Animate popped balloons: make the texture bigger/smaller over time through scale
            float alpha = Configuration.PopAnimationAlpha, beta = Configuration.PopAnimationBeta;
            float balloonScale = 1.0f + alpha * (float)Math.Sin(beta * popAnim.ElapsedSincePopped);
            popSize *= (balloonScale * Configuration.PopAnimationScale);

            // Scale the texture rectangle at its center and not at its top-left corner
            // like Draw() does when you pass a scaling factor.
            Vector2 popCenter = popAnim.Pos - (popSize * 0.5f);
            Rectangle popRect = new Rectangle((int)popCenter.X, (int)popCenter.Y,
                (int)popSize.X, (int)popSize.Y);
            spriteBatch.Draw(popAnim.PopTexture, popRect, Color.White);
        }

        private void DrawConfettiAnimation(GameTime gameTime, PopAnimation popAnim)
        {
            if (!Configuration.PopAnimationEnabled || !popAnim.PoppedByUser)
            {
                return;
            }

            float t = popAnim.ElapsedSincePopped / (Configuration.PopAnimationTime / 1000.0f);
            int frameIndex = (int)Math.Floor((t - Math.Truncate(t)) * confettiTextures.Count);
            Texture2D confettiTex = confettiTextures[frameIndex];
            Vector2 confettiSize = new Vector2(confettiTex.Width, confettiTex.Height) * Configuration.PopAnimationScale;
            Vector2 center = popAnim.Pos - (confettiSize * 0.5f);
            Rectangle confettiRect = new Rectangle((int)center.X, (int)center.Y,
                (int)confettiSize.X, (int)confettiSize.Y);
            spriteBatch.Draw(confettiTex, confettiRect, Color.White);
        }

        private void DrawBucket(GameTime gameTime, Bucket bucket)
        {
            Vector2 bucketPos = PhysicsManager.WorldBodyToPixel(bucket.Entity.Body.Position, PhysicsManager.WorldToPixel(bucket.Size));
            spriteBatch.Draw(bucket.Texture, bucketPos, Color.White);
        }

        private void DrawHand(GameTime gameTime, WorldEntity handBody)
        {
            Vector2 cursorPos = PhysicsManager.WorldBodyToPixel(handBody.Body.Position, new Vector2(handTexture.Width, handTexture.Height));
            Hand hand = physicsManager.GetHandForHandEntity(handBody);
            Color col = userColours[hand.ID % 2];
            SpriteEffects eff = hand.Side == Side.Left ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(handTexture, cursorPos, null, col, 0, Vector2.Zero, 1, eff, 0);
        }
        #endregion

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

        #region "Balloon Popping"
        private void PopBalloon(string balloonID, bool poppedByUser)
        {
            ClientBalloon balloon = balloons[balloonID];
            if (balloon == null)
            {
                throw new ArgumentOutOfRangeException("e", "No such balloon in received message.");
            }

            // Display content only asked and if balloon has a caption
            if (poppedByUser && balloon.ShouldDrawCaption())
            {
                contentBox.SetBalloon(balloon);
                physicsManager.DisableHandCollisions();
            }
            balloon.Popped = true;

            RemoveBalloon(balloon, true, poppedByUser);
        }

        /// <summary>
        /// Removes (immediately) the given balloon from the physics world and screen.
        /// </summary>
        /// <param name="balloon">Balloon to remove. </param>
        private void RemoveBalloon(ClientBalloon balloon, bool wasPopped, bool poppedByUser)
        {
            if (wasPopped)
            {
                // Create a new pop animation
                PopAnimation anim = new PopAnimation(balloon);
                anim.Pos = PhysicsManager.WorldToPixel(balloon.Entity.Body.Position);
                anim.TimePopped = currentTime.TotalGameTime;
                anim.PopTexture = balloonPopTextures[rng.Next(0, balloonPopTextures.Length)];
                anim.PoppedByUser = poppedByUser;
                popAnimations.Add(anim);
            }

            // Remove balloon from screen.
            physicsManager.RemoveEntity(balloon.Entity);
            balloon.Entity = null;
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
            if (plane.Entity != null)
            {
                physicsManager.RemoveEntity(plane.Entity);
                plane.Entity = null;
            }
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

            Balloon balloon = GetBalloonDetails(m.ObjectID);
            ClientBalloon b = new ClientBalloon(balloon);
            b.Texture = balloonTextures[balloon.Type][balloon.OverlayType];
            b.BalloonContentCache = contentBox.GetBalloonContent(b.ID);
            b.Entity = balloonEntity;

            // Render the balloon's caption if we already have it
            if (!String.IsNullOrWhiteSpace(b.Label))
            {
                contentBox.GenerateCaption(b);
            }

            balloons.Add(b.ID, b);

            // ask the server to send the balloon's content
            if (!balloonCache.ContainsKey(m.ObjectID))
            {
                balloonCache.Add(m.ObjectID, new Balloon(m.ObjectID));
                NetworkManager.RequestBalloonContent(m.ObjectID);
            }

            // ask the server to send up-to-date state
            // TODO: only do this if the details have been changed
            NetworkManager.RequestBalloonState(m.ObjectID);
        }

        public void OnNewPlane(NewPlaneMessage m)
        {
            // Choose where to place the plane
            Direction planeDirection;
            switch (m.Direction)
            {
            default:
            case Direction.Any:
                throw new Exception("Invalid direction for new plane");
            case Direction.Left:
                // The plane appears on the left side of the screen, it is going right
                planeDirection = Direction.Right;
                break;
            case Direction.Right:
                // The plane appears on the right side of the screen, it is going left
                planeDirection = Direction.Left;
                break;
            }

            Vector2 pixPosition = GetInitialPosition(m.Direction, m.Y,
                ClientPlane.PlaneSize.X * ClientPlane.PlaneScale);

            ClientPlane plane = new ClientPlane(m.ObjectID, m.PlaneType);
            plane.Velocity = new Vector2(m.Velocity.X, m.Velocity.Y);
            plane.Position = PhysicsManager.PixelToWorld(pixPosition);
            plane.Direction = planeDirection;
            plane.InitialY = m.Y;
            plane.Time = m.Time;
            plane.Entity = physicsManager.CreatePlane(plane.Position);
            planes.Add(plane.ID, plane);

            // start rendering the content
            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object o)
            {
                contentBox.GeneratePlaneCaption(plane);
            });
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
                PopBalloon(m.ObjectID, false);
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

            Balloon cached;
            if (balloonCache.TryGetValue(bcm.ObjectID, out cached))
            {
                cached.Label = bcm.Label;
                cached.Content = bcm.Content;
                cached.Type = bcm.BalloonType;
                cached.Url = bcm.Url;
                cached.ImageUrl = bcm.ImageUrl;
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

            Balloon cached;
            if (balloonCache.TryGetValue(bdm.ObjectID, out cached))
            {
                cached.OverlayType = bdm.OverlayType;
                cached.BackgroundColor = bdm.BackgroundColor;
                cached.Votes = bdm.Votes;
            }
        }

        /// <summary>
        /// Retrieves the details of a balloon from the Server.
        /// </summary>
        /// <param name="balloonID"></param>
        /// <returns></returns>
        private Balloon GetBalloonDetails(string balloonID)
        {
            Balloon balloon = null;
            if (balloonCache.TryGetValue(balloonID, out balloon))
            {
                return balloon;
            }
            return new Balloon(balloonID) { Label = "Test Label", Content = "Test Content", Type = BalloonType.CustomContent };
        }

        /// <summary>
        /// Notifies the Server that a balloon's details have changed 
        /// (usually its decoration).
        /// </summary>
        /// <param name="balloon"></param>
        private void UpdateBalloonDetails(Balloon balloon)
        {
            Balloon cachedBalloon = null;
            if (!balloonCache.TryGetValue(balloon.ID, out cachedBalloon))
            {
                cachedBalloon = new Balloon(balloon);
                balloonCache.Add(balloon.ID, cachedBalloon);
            }
            cachedBalloon.OverlayType = balloon.OverlayType;
            cachedBalloon.BackgroundColor = balloon.BackgroundColor;
            cachedBalloon.Votes = balloon.Votes;
            NetworkManager.UpdateBalloonState(balloon);
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
