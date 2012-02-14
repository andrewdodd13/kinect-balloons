using System;
using System.Collections.Generic;
using System.Linq;
using BubblesClient.Input.Controllers;
using BubblesClient.Model;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Balloons.Messaging.Model;
using FarseerPhysics.Dynamics.Contacts;

namespace BubblesClient.Physics
{
    public class PhysicsManager
    {
        public enum ExitPosition
        {
            Left, Right, Neither
        }

        public const float MeterInPixels = 64f;

        private World world;
        private Dictionary<Body, WorldEntity> entities = new Dictionary<Body, WorldEntity>();
        private Dictionary<Hand, WorldEntity> handBodies = new Dictionary<Hand, WorldEntity>();

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

        public void Initialize()
        {
            world = new World(new Vector2(0, -2));
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

        bool onBalloonCollision(Fixture fixtureA, Fixture fixtureB, FarseerPhysics.Dynamics.Contacts.Contact contact)
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
                //This is an acceptable case
                return true;
            }

            WorldEntity B = entities[fixtureB.Body];
            if (B.Type == WorldEntity.EntityType.Bucket)
            {
                if (BucketCollision != null) { BucketCollision(this, new BucketCollisionEventArgs { Balloon = A, Bucket = B }); }
            }
            else if (B.Type == WorldEntity.EntityType.Hand)
            {
                foreach (WorldEntity altHand in handBodies.Values)
                {
                    if (altHand != B)
                    {
                        //Magic number! Might need to adjust for sensitivity
                        //Also, it might be worth checking the velocity/momentum of the hands to check they are converving on the balloon
                        if (Vector2.Distance(new Vector2(altHand.Body.Position.X, altHand.Body.Position.Y), fixtureA.Body.Position) < 2)
                        {
                            if (BalloonPopped != null) { BalloonPopped(this, new BalloonPoppedEventArgs() { Balloon = A }); }
                        }
                    }
                }
            }

            return true;
        }
        #endregion

        public WorldEntity CreateRoof(int width, Vector2 position)
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

        private void CreateHandFixture(Hand hand)
        {
            Vector2 handPos = new Vector2(hand.Position.X, hand.Position.Y);
            Body handBody = BodyFactory.CreateRectangle(world, 1f, 1f, 1f, handPos / MeterInPixels);
            handBody.BodyType = BodyType.Dynamic;

            // Hands only collide with balloons for the moment
            handBody.OnCollision += delegate(Fixture fixtureA, Fixture fixtureB, Contact contact)
            {
                return (entities[fixtureB.Body].Type == WorldEntity.EntityType.Balloon);
            };

            FixedMouseJoint handJoint = new FixedMouseJoint(handBody, handBody.Position);
            handJoint.MaxForce = 1000f;
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
        #endregion

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
