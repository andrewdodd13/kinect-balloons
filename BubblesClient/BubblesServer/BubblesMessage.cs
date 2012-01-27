using System;

namespace BubblesServer
{
    public enum BubblesMessageType
    {
        Add,
        ChangeScreen,
        GetInfo,
        Update,
        Pop
    }
    
    public class BubblesMessage
    {
        private BubblesMessageType m_type;
        
        public BubblesMessageType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        
        public BubblesMessage(BubblesMessageType type)
        {
            m_type = type;
        }
    }
    
    public class AddMessage : BubblesMessage
    {
        private int m_bubbleID;
        
        public int BubbleID {
            get { return this.m_bubbleID; }
            set { m_bubbleID = value; }
        }
        
        public AddMessage(int bubbleID) : base(BubblesMessageType.Add)
        {
            m_bubbleID = bubbleID;
        }
    }
    
    public class ChangeScreenMessage : BubblesMessage
    {
        private int m_bubbleID;
        private ScreenDirection m_direction;

        public ScreenDirection Direction {
            get {
                return this.m_direction;
            }
        }        
        public int BubbleID {
            get { return this.m_bubbleID; }
            set { m_bubbleID = value; }
        }
        
        public ChangeScreenMessage(int bubbleID, ScreenDirection direction)
            : base(BubblesMessageType.ChangeScreen)
        {
            m_bubbleID = bubbleID;
            m_direction = direction;
        }
    }
}