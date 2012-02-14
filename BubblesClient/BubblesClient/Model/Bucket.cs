namespace BubblesClient.Model
{
    using FarseerPhysics.Dynamics;
    using Microsoft.Xna.Framework;
    using BubblesClient.Physics;
    using Microsoft.Xna.Framework.Graphics;

    public class Bucket
    {
        public int ID { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public WorldEntity Entity { get; set; }
        public Texture2D Texture { get; set; }

        public const int BucketWidth = 128;
        public const int BucketHeight = 128;
    }
}
