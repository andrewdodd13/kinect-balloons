using System;

namespace BubblesServer
{
    public enum BubblesMessageType
    {
        NewBubble,
        GetBubbleInfo
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
    
    public class NewBubbleNotification : BubblesMessage
    {
        private int m_bubbleID;
        
        public int BubbleID {
            get { return this.m_bubbleID; }
            set { m_bubbleID = value; }
        }
        
        public NewBubbleNotification(int bubbleID)
            : base(BubblesMessageType.NewBubble)
        {
            m_bubbleID = bubbleID;
        }
    }
}