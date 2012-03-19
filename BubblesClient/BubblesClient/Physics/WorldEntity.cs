using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;

namespace BubblesClient.Physics
{
    /// <summary>
    /// WorldEntity represents an object as it exists in the physics world.
    /// </summary>
    public class WorldEntity
    {
        public enum EntityType { Balloon, Plane, Bucket, Hand, Landscape };

        public Body Body { get; private set; }
        public Joint Joint { get; private set; }
        public EntityType Type { get; private set; }

        public WorldEntity(Body body, EntityType type)
        {
            this.Body = body;
            this.Type = type;
            this.Joint = null;
        }

        public WorldEntity(Body body, Joint joint, EntityType type)
        {
            this.Body = body;
            this.Type = type;
            this.Joint = joint;
        }
    }
}
