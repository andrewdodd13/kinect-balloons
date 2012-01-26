using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BubblesClient.Model;
using Microsoft.Xna.Framework;

namespace BubblesClient.Network.Event
{
    public class NewBalloonEvent
    {
        private Balloon _balloon;
        private Vector2 _position;
        private Vector2 _velocity;

        public Balloon Balloon { get { return _balloon; } }
        public Vector2 Position { get { return _position; } }
        public Vector2 Velocity { get { return _velocity; } }

        public NewBalloonEvent(Balloon balloon, Vector2 position, Vector2 velocity)
        {
            _balloon = balloon;
            _position = position;
            _velocity = velocity;
        }
    }
}
