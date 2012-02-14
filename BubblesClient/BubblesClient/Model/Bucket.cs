namespace BubblesClient.Model
{
    using FarseerPhysics.Dynamics;
    using Microsoft.Xna.Framework;
    using BubblesClient.Physics;

    public class Bucket
    {
        public int ID { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public WorldEntity Entity { get; set; }
    }
}
