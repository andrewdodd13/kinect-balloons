using Microsoft.Xna.Framework;

namespace BubblesClient.Input
{
    public enum Side { Right, Left }
    /// <summary>
    /// Hand is a mutable identifier for a user's hand. It's used by the system
    /// to keep state of hands between frames.
    /// </summary>
    public class Hand
    {
        public Vector3 Position { get; set; }
        public Side Side { get; set; }
        public int ID { get; set; }
    }
}
