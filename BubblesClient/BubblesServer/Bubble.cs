using System;

namespace BubblesServer
{
    public class Bubble
    {
        private int m_id;
        private Screen m_screen;

        public int ID
        {
            get
            {
                return this.m_id;
            }
        }

        public Screen Screen
        {
            get
            {
                return this.m_screen;
            }
            set
            {
                m_screen = value;
            }
        }
        
        public Bubble(int id)
        {
            m_id = id;
        }
    }
}

