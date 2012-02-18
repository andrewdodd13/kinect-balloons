using System;
using System.Collections.Generic;
using Balloons;
using Balloons.Messaging;
using Balloons.Messaging.Model;

namespace Balloons.Server
{
	public class Screen
	{
        private readonly int m_id;
        private ScreenConnection m_connection;
        private Dictionary<string, ServerBalloon> m_balloons;
        
        public Screen(int id, ScreenConnection connection)
        {
            m_id = id;
            m_connection = connection;
            m_balloons = new Dictionary<string, ServerBalloon>();
        }
     
        public int ID
        {
            get { return m_id; }
        }
     
        public ScreenConnection Connection
        {
            get { return m_connection; }
        }
        
        public Dictionary<string, ServerBalloon> Balloons
        {
            get { return m_balloons; }
        }
	}
}
