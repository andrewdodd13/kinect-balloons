using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BubblesClient.Network.Event;
using Microsoft.Xna.Framework;
using BubblesClient.Model;

namespace BubblesClient.Network
{
    class MockNetworkManager : INetworkEventManager
    {
        private List<NewBalloonEvent> _balloonEvents;

        public MockNetworkManager()
        {
            _balloonEvents = new List<NewBalloonEvent>();
            _balloonEvents.Add(new NewBalloonEvent(new Model.Balloon("http://www.google.com", "Mock", "Mock 1"), Vector2.Zero, Vector2.Zero));
            _balloonEvents.Add(new NewBalloonEvent(new Model.Balloon("http://www.google.com", "Mock", "Mock 2"), Vector2.Zero, Vector2.Zero));
            _balloonEvents.Add(new NewBalloonEvent(new Model.Balloon("http://www.google.com", "Mock", "Mock 3"), Vector2.Zero, Vector2.Zero));
            _balloonEvents.Add(new NewBalloonEvent(new Model.Balloon("http://www.google.com", "Mock", "Mock 4"), Vector2.Zero, Vector2.Zero));
            _balloonEvents.Add(new NewBalloonEvent(new Model.Balloon("http://www.google.com", "Mock", "Mock 5"), Vector2.Zero, Vector2.Zero));
        }

        public List<NewBalloonEvent> GetNewBalloons()
        {
            List<NewBalloonEvent> copy = new List<NewBalloonEvent>(_balloonEvents);

            _balloonEvents.Clear();

            if(new Random().Next(300) == 0) 
            {
                _balloonEvents.Add(new NewBalloonEvent(new Balloon("http://www.google.com", "Mock", "Mock " + DateTime.Now), Vector2.Zero, Vector2.Zero));
            }

            return copy;
        }

        public List<Model.Balloon> GetPoppedBalloons()
        {
            throw new NotImplementedException();
        }

        public void NotifyBalloonPop(Model.Balloon balloon)
        {
            throw new NotImplementedException();
        }

        public void NotifyBalloonExit(Model.Balloon balloon, Microsoft.Xna.Framework.Vector2 position, Microsoft.Xna.Framework.Vector2 velocity)
        {
            throw new NotImplementedException();
        }

        public void NotifyBalloonUpdated(Model.Balloon balloon)
        {
            throw new NotImplementedException();
        }
    }
}
