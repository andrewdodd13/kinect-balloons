using System;
using System.Collections.Generic;
using System.Linq;
using Balloons.Messaging.Model;
using BubblesClient.Input.Controllers;
using BubblesClient.Model;
using BubblesClient.Model.Buckets;
using BubblesClient.Physics;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        private Texture2D[] balloonPopTextures;

        private Texture2D boxTexture;
        private SpriteFont contentFont, summaryFont;

        private Dictionary<string, BalloonContentCache> balloonTextureCache = new Dictionary<string, BalloonContentCache>();

        // Network
        public ScreenManager ScreenManager { get; private set; }

        // XNA Graphics
        private GraphicsDeviceManager graphics;
        private Vector2 screenDimensions;
        private SpriteBatch spriteBatch;

        // Input
        private IInputController input;
        private Color[] userColours = { Color.Red, Color.Blue }; // Move this to config?

        // Physics World
        private Dictionary<string, ClientBalloon> balloons = new Dictionary<string, ClientBalloon>();
        private List<Bucket> buckets = new List<Bucket>();
        private PhysicsManager physicsManager = new PhysicsManager();

        private Dictionary<ClientBalloon, WorldEntity> balloonEntities = new Dictionary<ClientBalloon, WorldEntity>();

        //other stuff
        private bool showBuckets = true;
        private Bucket oldBucket = null;

        // If this is not null then we will be showing a balloon. We really need a state machine.
        private ClientBalloon poppedBalloon = null;
        private List<PopAnim> popAnimations = new List<PopAnim>();
        private GameTime currentTime;

        public BubblesClientGame(ScreenManager screenManager, IInputController controller)
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
                PopBalloon(b.ID, true);
            };
            physicsManager.BucketCollision += delegate(object o, PhysicsManager.BucketCollisionEventArgs args)
            {
                ClientBalloon balloon = balloonEntities.First(x => x.Value == args.Balloon).Key;
                Bucket bucket = buckets.First(x => x.Entity == args.Bucket);
                this.ApplyBucketToBalloon(bucket, balloon);
            };

            // Initialise network
            this.ScreenManager = screenManager;
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

            // Create a roof and floor
            physicsManager.CreateBoundary((int)screenDimensions.X * 4, new Vector2(screenDimensions.X / 2, 0));
            physicsManager.CreateBoundary((int)screenDimensions.X * 4, new Vector2(screenDimensions.X / 2, screenDimensions.Y));

            // Load buckets
            float gapBetweenBuckets = (screenDimensions.X - (Bucket.BucketWidth * 5)) / 6;

            for (int i = 0; i < buckets.Count; i++)
            {
                float x = (i + 1) * gapBetweenBuckets + (i + 0.5f) * Bucket.BucketWidth;
                float y = screenDimensions.Y - Bucket.BucketHeight;

                Bucket b = buckets[i];
                b.Position = PhysicsManager.PixelToWorld(new Vector2(x, y));
                b.Size = PhysicsManager.PixelToWorld(new Vector2(Bucket.BucketWidth, Bucket.BucketHeight));
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

            contentFont = Content.Load<SpriteFont>("Fonts/SpriteFontSmall");
            summaryFont = Content.Load<SpriteFont>("Fonts/SpriteFontLarge");

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

            balloonPopTextures = new Texture2D[] {
                Content.Load<Texture2D>("Images/BalloonPop")
            };

            boxTexture = Content.Load<Texture2D>("Images/Box");

            // Create buckets
            buckets.Add(new ColourBucket(Content.Load<Texture2D>("Images/BucketRed"), Color.Red));
            buckets.Add(new DecorationBucket(Content.Load<Texture2D>("Images/bucketSpots"), OverlayType.Spots));
            buckets.Add(new ColourBucket(Content.Load<Texture2D>("Images/bucketGreen"), Color.Green));
            buckets.Add(new DecorationBucket(Content.Load<Texture2D>("Images/bucketStripes"), OverlayType.Stripes));
            buckets.Add(new ColourBucket(Content.Load<Texture2D>("Images/bucketBlue"), Color.Blue));
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
            currentTime = gameTime;

            // Query the Network Manager for events
            ProcessNetworkMessages();

            // Query the Input Library if there isn't currently a message displayed.
            if (poppedBalloon == null)
            {
                this.HandleInput();
            }
            else
            {
                if (input.ShouldClosePopup())
                {
                    poppedBalloon = null;
                }
            }

            physicsManager.ApplyWind();
            physicsManager.Update(gameTime);

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

            removals.ForEach(x => RemoveBalloon(x));

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
            for(int i = popAnimations.Count - 1; i >= 0; i--)
            {
                double elapsedMs = gameTime.TotalGameTime.TotalMilliseconds - popAnimations[i].TimePopped.TotalMilliseconds;
                if(elapsedMs >= Configuration.PopAnimationTime)
                {
                    popAnimations.RemoveAt(i);
                }
                else
                {
                    popAnimations[i].ElapsedSincePopped = (float)(elapsedMs / 1000.0);
                }
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

            // Draw all of the boxes first
            foreach (ClientBalloon balloon in balloons.Values)
            {
                // Draw the box containing the balloon text if it is not a user-customized balloon
                if (IsCaptionDrawn(balloon))
                {
                    Vector2 boxPosition = PhysicsManager.WorldToPixel(balloonEntities[balloon].Body.Position) - new Vector2(boxTexture.Width / 2, 0);
                    boxPosition.Y += balloon.Texture.Height - (ClientBalloon.BalloonHeight / 2);

                    spriteBatch.Draw(boxTexture, boxPosition, Color.White);

                    // If the label is not cached then it means it has not
                    // been formatted to fit in the box; therefore format it 
                    // and save it back
                    if (!balloon.IsLabelCached)
                    {
                        string labelText = balloon.Label;
                        balloon.Label = wrapText(summaryFont, labelText, new Vector2(boxTexture.Width, boxTexture.Height));

                        if (String.IsNullOrEmpty(balloon.Content) || (balloon.Content.Trim() == string.Empty))
                        {
                            balloon.Content = wrapText(contentFont, labelText, new Vector2(contentBox.Width - (24 * 3) - 224, contentBox.Height - (24 * 2)));
                        }
                        else
                        {
                            balloon.Content = wrapText(contentFont, balloon.Content, new Vector2(contentBox.Width - (24 * 3) - 224, contentBox.Height - (24 * 2)));
                        }
                        balloon.IsLabelCached = true;
                    }

                    drawTextLabel(summaryFont, balloon.Label, new Vector2(boxPosition.X, boxPosition.Y));
                }
            }

            // Draw all of the balloons
            foreach (ClientBalloon balloon in balloons.Values)
            {
                Vector2 balloonPosition = PhysicsManager.WorldBodyToPixel(balloonEntities[balloon].Body.Position, new Vector2(balloon.Texture.Width, ClientBalloon.BalloonHeight));
                Color balloonColour = new Color(balloon.BackgroundColor.Red, balloon.BackgroundColor.Green, balloon.BackgroundColor.Blue, balloon.BackgroundColor.Alpha);
                spriteBatch.Draw(balloon.Texture, balloonPosition, balloonColour);
            }

            // Draw all pop animations
            foreach(PopAnim popAnim in popAnimations)
            {
                Rectangle textureRect = new Rectangle((int)popAnim.Pos.X, (int)popAnim.Pos.Y,
                                                popAnim.PopTexture.Width, popAnim.PopTexture.Height);
                Color popColour = new Color(popAnim.PopColour.Red, popAnim.PopColour.Green, popAnim.PopColour.Blue, popAnim.PopColour.Alpha);
                if(Configuration.PopAnimationEnabled)
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

            //display content page if balloonPopped is true (should only be true for 30 seconds)
            if (poppedBalloon != null)
            {
                // Position contains the co-ordinate of the top-left corner of the box
                Vector2 position = (screenDimensions / 2) - (new Vector2(contentBox.Width, contentBox.Height) / 2);

                // Draw the box itself
                spriteBatch.Draw(contentBox, position, Color.White);

                // Draw the text
                drawTextLabel(contentFont, poppedBalloon.Content, position + new Vector2(24, 24));

                // Draw the QR Code
                if(poppedBalloon.BalloonContentCache.QRCode != null)
                {
                    spriteBatch.Draw(poppedBalloon.BalloonContentCache.QRCode,
                        position + new Vector2(contentBox.Width - 280, 24), Color.White);
                }

                // Draw the Image
                Texture2D balloonImage = poppedBalloon.BalloonContentCache.Image;
                spriteBatch.Draw(balloonImage, position + new Vector2(contentBox.Width - 280, contentBox.Height - balloonImage.Height - 24), Color.White);
            }
            else
            {
                // Draw all of the registered hands
                foreach (WorldEntity handBody in physicsManager.GetHandPositions())
                {
                    Vector2 cursorPos = PhysicsManager.WorldBodyToPixel(handBody.Body.Position, new Vector2(handTexture.Width, handTexture.Height));
                    Hand hand = physicsManager.GetHandForHandEntity(handBody);
                    Color col = userColours[hand.ID%2];
                    SpriteEffects eff = hand.Side == Side.Left ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                    spriteBatch.Draw(handTexture, cursorPos, null, col, 0, Vector2.Zero, 1, eff, 0);
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private bool IsCaptionDrawn(ClientBalloon balloon)
        {
            return balloon.Type != BalloonType.Customizable &&
                !balloon.Popped &&
                !String.IsNullOrWhiteSpace(balloon.Label);
        }

        private void HandleInput()
        {
            physicsManager.UpdateHandPositions(input.GetHandPositions());
        }

        /// <summary>
        /// This function takes a string and a vector2. It splits the string given by spaces
        /// and then for each word it will check the length of it against the length of the
        /// vector2 given in. When the word is passed the edge a new line is put in.
        /// </summary>
        /// <param name="font">The font to use for measuments</param>
        /// <param name="text">Text to be wrapped</param>
        /// <param name="containerDemensions">Dimensions of the container the text is to be wrapped in</param>
        /// <returns>The text including newlines to fit into the container</returns>
        private String wrapText(SpriteFont font, String text, Vector2 containerDemensions)
        {
            String line = String.Empty;
            String returnString = String.Empty;
            String[] wordArray = text.Split(' ');

            foreach (String word in wordArray)
            {
                if (font.MeasureString(line + word).Length() > containerDemensions.X)
                {
                    returnString += line + '\n';
                    line = String.Empty;

                    // If the string is longer than the box, we need to stop
                    if (font.MeasureString(returnString).Y > containerDemensions.Y)
                    {
                        break;
                    }
                }

                line += word + ' ';
            }

            return returnString + line;
        }

        private void drawTextLabel(SpriteFont font, String text, Vector2 pos)
        {
            try
            {
                spriteBatch.DrawString(font, text, pos, Color.Black);
            }
            catch (Exception)
            {
                spriteBatch.DrawString(font, "Invalid character", pos, Color.Red);
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
                ScreenManager.UpdateBalloonDetails(balloon);
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
            showContent &= IsCaptionDrawn(balloon);
            balloon.Popped = true;
            if (showContent)
            {
                poppedBalloon = balloon;
                ScreenManager.CallLater(Configuration.MessageDisplayTime, delegate()
                {
                    poppedBalloon = null;
                });
            }

            RemoveBalloon(balloon);
        }

        /// <summary>
        /// Removes (immediately) the given balloon from the physics world and screen.
        /// </summary>
        /// <param name="balloon">Balloon to remove. </param>
        private void RemoveBalloon(ClientBalloon balloon)
        {
            // Create a new pop animation
            PopAnim anim = new PopAnim(balloon);
            anim.Pos = PhysicsManager.WorldToPixel(balloonEntities[balloon].Body.Position);
            anim.TimePopped = currentTime.TotalGameTime;
            anim.PopTexture = balloonPopTextures[new Random().Next(0, balloonPopTextures.Length)];
            anim.PopColour = new Colour(255, 255, 255, 255);
            popAnimations.Add(anim);

            // Remove balloon from screen.
            physicsManager.RemoveEntity(balloonEntities[balloon]);
            balloonEntities.Remove(balloon);
            balloons.Remove(balloon.ID);

            // Remove balloon from server. This must be done after removing the balloon
            // from the map or an exception can be raised when the server sends a
            // "new balloon" message with the same ID when the feed is updated.
            ScreenManager.NotifyBalloonPopped(balloon);
        }
        #endregion

        #region "Networking"
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
                    case MessageType.BalloonStateUpdate:
                        OnBalloonStateUpdate((BalloonStateUpdateMessage)msg);
                        break;
                    case MessageType.Callback:
                        var cm = (CallbackMessage)msg;
                        cm.Callback();
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
            b.Texture = balloonTextures[balloon.Type][balloon.OverlayType];

            // Get the images from the cache
            if (!balloonTextureCache.ContainsKey(b.ID))
            {
                BalloonContentCache cacheEntry = new BalloonContentCache()
                {
                    ID = b.ID,
                    QRCode = String.IsNullOrEmpty(b.Url) ? null :
                        ImageGenerator.GenerateQRCode(graphics.GraphicsDevice, b.Url),
                    Image = ImageGenerator.GenerateFromWeb(graphics.GraphicsDevice, b.ImageUrl)
                };

                balloonTextureCache.Add(b.ID, cacheEntry);
            }

            b.BalloonContentCache = balloonTextureCache[b.ID];

            balloons.Add(b.ID, b);
            balloonEntities[b] = balloonEntity;
        }

        /// <summary>
        /// Handles the case where the server forces us to pop a balloon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnPopBalloon(PopBalloonMessage m)
        {
            if (balloons.ContainsKey(m.BalloonID))
            {
                PopBalloon(m.BalloonID);
            }
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
                balloon.ImageUrl = bcm.ImageUrl;
            }
        }

        public void OnBalloonStateUpdate(BalloonStateUpdateMessage bdm)
        {
            ClientBalloon balloon;
            if (balloons.TryGetValue(bdm.BalloonID, out balloon))
            {
                balloon.OverlayType = bdm.OverlayType;
                balloon.BackgroundColor = bdm.BackgroundColor;
                balloon.Votes = bdm.Votes;
            }
        }
        #endregion
    }
}
