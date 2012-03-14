using System;
using Balloons.Messaging.Model;
using BubblesClient.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BubblesClient.Model
{
    /// <summary>
    /// ClientPlane is an extension of Plane which contains details 
    /// specific to displaying the plane on the client side.
    /// </summary>
    public class ClientPlane : Balloons.Messaging.Model.Plane
    {
        /// <summary>
        /// This is true if the plane's position is outside of the screen and
        /// the server has been notified of it.
        /// </summary>
        public bool OffScreen;

        public static Vector2 PlaneSize = new Vector2(380f, 200f);
        public const float PlaneScale = 0.7f;

        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }

        public float InitialY { get; set; }
        public Direction Direction { get; set; }

        public WorldEntity Entity { get; set; }

        public System.Drawing.Bitmap Caption { get; set; }
        public Texture2D CaptionTexture { get; set; }

        public ClientPlane(string id, PlaneType type)
            : base(id, type)
        {
        }

        public ClientPlane(Balloons.Messaging.Model.Plane parent)
            : base(parent)
        {
        }
    }
}
