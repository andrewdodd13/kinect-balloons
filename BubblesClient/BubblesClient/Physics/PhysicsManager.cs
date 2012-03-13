using System;
using System.Collections.Generic;
using System.Linq;
using Balloons.Messaging.Model;
using BubblesClient.Input;
using BubblesClient.Model;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;

namespace BubblesClient.Physics
{
    /// <summary>
    /// Physics Manager handles the creation and management of entities inside
    /// the physics world and provides events which affect the entire system
    /// such as balloon popping.
    /// </summary>
    public class PhysicsManager
    {
        public const float MeterInPixels = 64f;

        private World world;
        private Dictionary<Body, WorldEntity> entities = new Dictionary<Body, WorldEntity>();
        private Dictionary<Hand, WorldEntity> handBodies = new Dictionary<Hand, WorldEntity>();

        private bool handCollisionsEnabled = true;

        private Random rnd = new Random();

        private float handSize = 1f;

        public event EventHandler<BalloonPoppedEventArgs> BalloonPopped;
        public class BalloonPoppedEventArgs : EventArgs
        {
            public WorldEntity Balloon { get; set; }
        }

        public event EventHandler<BucketCollisionEventArgs> BucketCollision;
        public class BucketCollisionEventArgs : EventArgs
        {
            public WorldEntity Balloon { get; set; }
            public WorldEntity Bucket { get; set; }
        }

        public void Initialize(int handSize)
        {
            world = new World(new Vector2(0, 1));

            this.handSize = (handSize / MeterInPixels) / 2f;
        }

        public void Update(GameTime gameTime)
        {
            world.Step((float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f);
        }

        #region "Balloon Handling"
        public WorldEntity CreateBalloon(Vector2 position, Vector2 velocity)
        {
            float circleRadius = ClientBalloon.BalloonWidth / (2f * MeterInPixels);

            Body balloonBody = BodyFactory.CreateCircle(world, circleRadius, 1f, PixelToWorld(position));
            balloonBody.BodyType = BodyType.Dynamic;
            balloonBody.Restitution = 0.3f;
            balloonBody.Friction = 0.5f;
            balloonBody.LinearDamping = 1.0f;

            balloonBody.ApplyLinearImpulse(velocity * balloonBody.Mass);
            balloonBody.OnCollision += new OnCollisionEventHandler(onBalloonCollision);

            WorldEntity entity = new WorldEntity(balloonBody, WorldEntity.EntityType.Balloon);
            entities.Add(balloonBody, entity);

            return entity;
        }

        private bool onBalloonCollision(Fixture fixtureA, Fixture fixtureB, FarseerPhysics.Dynamics.Contacts.Contact contact)
        {
            if (!entities.ContainsKey(fixtureA.Body))
            {
                Console.WriteLine("Error: a balloon was not in the objects dictionary");
                return true;
            }

            WorldEntity A = entities[fixtureA.Body];
            if (A.Type != WorldEntity.EntityType.Balloon)
            {
                Console.WriteLine("Error: balloon collide event attached to non-balloon body");
                return true;
            }
            if (!entities.ContainsKey(fixtureB.Body))
            {
                // This is an acceptable case
                return true;
            }

            WorldEntity B = entities[fixtureB.Body];
            if (B.Type == WorldEntity.EntityType.Bucket)
            {
                if (BucketCollision != null)
                {
                    BucketCollision(this, new BucketCollisionEventArgs { Balloon = A, Bucket = B });
                }
            }
            else if (B.Type == WorldEntity.EntityType.Hand && handCollisionsEnabled)
            {
                float movementThreshold = Configuration.KinectMovementThreshold;
                float altHandRange = Configuration.KinectMaxHandRange;
                double minimumAttackAngle = Configuration.KinectMinAttackAngle;

                // First we get the hand that has collided with the balloon and check that it is
                // moving fast enough and at a direct enough angle towards the balloon to trigger the clap
                Hand _hand1 = GetHandForHandEntity(B);
                Vector2 handToBalloon = A.Body.Position - B.Body.Position;
                Vector2 handDirection = B.Body.LinearVelocity;

                float velocity = handDirection.Length();

                handToBalloon.Normalize();
                handDirection.Normalize();
                double theta = Vector2.Dot(handToBalloon, handDirection);

                if (velocity > movementThreshold && theta > minimumAttackAngle)
                {
                    foreach (WorldEntity altHand in handBodies.Values)
                    {
                        // Check this isn't already the hand we know about
                        if (altHand == B)
                        {
                            continue;
                        }

                        Hand _hand2 = GetHandForHandEntity(altHand);
                        if (Configuration.EnableHighFive && _hand1.ID != _hand2.ID)
                        {
                            // Don't allow hands belonging to different users to trigger claps
                            continue;
                        }

                        // First check if the second hand is close enough to the balloon
                        float distanceFromAltHandToBallon = Vector2.Distance(new Vector2(altHand.Body.Position.X, altHand.Body.Position.Y), fixtureA.Body.Position);
                        if (distanceFromAltHandToBallon < altHandRange)
                        {
                            // Now check if the second hand is moving fast enough
                            if (altHand.Body.LinearVelocity.Length() < movementThreshold)
                            {
                                continue;
                            }

                            // Now check if the second hand is moving towards the balloon
                            Vector2 altHandToBalloon = A.Body.Position - altHand.Body.Position;
                            altHandToBalloon.Normalize();
                            Vector2 altHandDirection = altHand.Body.LinearVelocity;
                            altHandDirection.Normalize();
                            double altTheta = Vector2.Dot(altHandToBalloon, altHandDirection);

                            if (altTheta < minimumAttackAngle)
                            {
                                continue;
                            }

                            // Now check the hands are moving towards each other
                            double omega = Vector2.Dot(altHandDirection, handDirection);
                            if (omega > -minimumAttackAngle)
                            {
                                continue;
                            }

                            // Phew - if we got through all that, we've detected a clap!
                            if (BalloonPopped != null)
                            {
                                BalloonPopped(this, new BalloonPoppedEventArgs() { Balloon = A });
                            }
                        }
                    }
                }
            }
            else if (!handCollisionsEnabled)
            {
                return false;
            }

            return true;
        }
        #endregion

        public WorldEntity CreateBoundary(int width, Vector2 position)
        {
            Body roofBody = BodyFactory.CreateRectangle(world, width / MeterInPixels, 1 / MeterInPixels, 1f, position / MeterInPixels);
            roofBody.IsStatic = true;
            roofBody.Restitution = 0.3f;
            roofBody.Friction = 1f;

            WorldEntity entity = new WorldEntity(roofBody, WorldEntity.EntityType.Landscape);
            entities.Add(roofBody, entity);
            return entity;
        }

        public WorldEntity CreateBucket(Vector2 size, Vector2 position)
        {
            Body body = BodyFactory.CreateRectangle(world, size.X, size.Y, 0.1f, position);
            WorldEntity entity = new WorldEntity(body, WorldEntity.EntityType.Bucket);
            entities.Add(body, entity);
            return entity;
        }

        public WorldEntity CreatePlane(Vector2 position)
        {
            float planeWidth = ClientPlane.PlaneWidth / MeterInPixels;
            float planeHeight = ClientPlane.PlaneHeight / MeterInPixels;
            Body planeBody = BodyFactory.CreateRectangle(world, planeWidth, planeHeight, 1f, position);
            WorldEntity entity = new WorldEntity(planeBody, WorldEntity.EntityType.Plane);
            entities.Add(planeBody, entity);
            return entity;
        }

        #region "Hand handling"
        public void UpdateHandPositions(Hand[] hands)
        {
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
            foreach (KeyValuePair<Hand, WorldEntity> handBody in handBodies)
            {
                handBody.Value.Joint.WorldAnchorB = new Vector2(handBody.Key.Position.X, handBody.Key.Position.Y) / MeterInPixels;
            }
        }

        public List<WorldEntity> GetHandPositions()
        {
            return new List<WorldEntity>(handBodies.Values);
        }

        public Hand GetHandForHandEntity(WorldEntity ent)
        {
            if (ent.Type != WorldEntity.EntityType.Hand)
                return null;

            foreach (Hand hand in handBodies.Keys)
            {
                if (handBodies[hand] == ent)
                    return hand;
            }

            return null;
        }

        private void CreateHandFixture(Hand hand)
        {
            Vector2 handPos = new Vector2(hand.Position.X, hand.Position.Y);
            Body handBody = BodyFactory.CreateCircle(world, handSize, 1f, handPos / MeterInPixels);
            handBody.BodyType = BodyType.Dynamic;

            // Check what hands should be colliding with
            if (handCollisionsEnabled)
            {
                handBody.OnCollision += HandCollisionCheckDelegate;
            }
            else
            {
                handBody.OnCollision += HandCollisionFalseDelegate;
            }

            FixedMouseJoint handJoint = new FixedMouseJoint(handBody, handBody.Position);
            handJoint.MaxForce = 10000f;
            world.AddJoint(handJoint);

            WorldEntity handEntity = new WorldEntity(handBody, handJoint, WorldEntity.EntityType.Hand);
            handBodies.Add(hand, handEntity);
            entities.Add(handBody, handEntity);
        }

        private void RemoveHandFixture(Hand hand)
        {
            WorldEntity bodyEntity = handBodies[hand];
            world.RemoveJoint(bodyEntity.Joint);
            world.RemoveBody(bodyEntity.Body);

            handBodies.Remove(hand);
            entities.Remove(bodyEntity.Body);
        }

        public void EnableHandCollisions()
        {
            lock (handBodies)
            {
                foreach (WorldEntity entity in handBodies.Values)
                {
                    entity.Body.OnCollision -= HandCollisionFalseDelegate;
                    entity.Body.OnCollision += HandCollisionCheckDelegate;
                }
                handCollisionsEnabled = true;
            }
        }

        public void DisableHandCollisions()
        {
            lock (handBodies)
            {
                foreach (WorldEntity entity in handBodies.Values)
                {
                    entity.Body.OnCollision += HandCollisionFalseDelegate;
                    entity.Body.OnCollision -= HandCollisionCheckDelegate;
                }
                handCollisionsEnabled = false;
            }
        }

        private bool HandCollisionFalseDelegate(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            return false;
        }

        private bool HandCollisionCheckDelegate(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            WorldEntity entity;
            if (entities.TryGetValue(fixtureB.Body, out entity))
            {
                return (entity.Type == WorldEntity.EntityType.Balloon);
            }
            return false;
        }

        #endregion

        public void ApplyWind()
        {
            Vector2 windForce = new Vector2(4, 0);
            // Hmm, is there a better way to get the balloon & bodies?
            foreach (Body body in entities.Keys)
            {
                WorldEntity entity = entities[body];
                if (entity.Type == WorldEntity.EntityType.Balloon)
                {
                    // Apply buoyant force
                    body.ApplyForce(new Vector2(0, -7));

                    // Apply roof repelant force
                    int jiggleForce = 1300; // Increasing jiggleForce makes the balloons less likely to reach equilibrium along the roof
                    if (body.Position.Y < 1.5)
                    {
                        body.ApplyForce(new Vector2(0, 200 + 10 * (2 - body.Position.Y) + rnd.Next(jiggleForce)));
                    }

                    // Apply anti-dead zone force
                    float maxx = 1360 / MeterInPixels; // MAGIC NUMBER OMGWTF
                    // We calculate the direction of the wind, and use it to apply either a sucking or repelling deadzone force
                    float windDir = windForce.X < 0 ? 1 : -1;
                    if (body.Position.X < 0)
                    {
                        body.ApplyForce(new Vector2(windDir * 5 * body.Position.X, 0));
                    }
                    if (body.Position.X > maxx)
                    {
                        body.ApplyForce(new Vector2(windDir * 5 * (maxx - body.Position.X), 0));
                    }

                    // Apply wind
                    body.ApplyForce(windForce);
                }
            }
        }

        /// <summary>
        /// Removes an entity from the physics world.
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        public void RemoveEntity(WorldEntity entity)
        {
            if (!entities.ContainsKey(entity.Body))
            {
                return;
            }

            if (entity.Joint != null)
            {
                world.RemoveJoint(entity.Joint);
            }
            if (entity.Body != null)
            {
                world.RemoveBody(entity.Body);
            }
            entities.Remove(entity.Body);
        }

        public static Vector2 WorldToPixel(Vector2 worldPosition)
        {
            return worldPosition * MeterInPixels;
        }

        public static Vector2 PixelToWorld(Vector2 pixelPosition)
        {
            return pixelPosition / MeterInPixels;
        }

        public static Vector2 WorldBodyToPixel(Vector2 worldPosition, Vector2 pixelOffset)
        {
            return (worldPosition * MeterInPixels) - (pixelOffset / 2);
        }

        public static Vector2 PixelToWorldBody(Vector2 pixelPosition, Vector2 pixelOffset)
        {
            return (pixelPosition / MeterInPixels) + ((pixelOffset / MeterInPixels) / 2);
        }
    }
}
