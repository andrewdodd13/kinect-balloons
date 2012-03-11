namespace BubblesClient.Model
{
    using BubblesClient.Physics;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// Bucket represents a paint bucket which can be used to apply some form
    /// of decoration to a balloon. Exactly what happens on application should
    /// be defined in a child class.
    /// </summary>
    public abstract class Bucket
    {
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public WorldEntity Entity { get; set; }
        public Texture2D Texture { get; set; }

        public const int BucketWidth = 128;
        public const int BucketHeight = 128;

        public abstract void ApplyToBalloon(ClientBalloon balloon);

        public Bucket(Texture2D texture)
        {
            this.Texture = texture;
        }
    }
}
