using System;
using System.Net.Sockets;

namespace BubblesServer
{
    /// <summary>
    /// List of all message types.
    /// </summary>
    public enum MessageType
    {
        Add,
        ChangeScreen,
        GetInfo,
        Update,
        Pop,
        Connected,
        Disconnected
    }
    
    /// <summary>
    /// Message that can be sent between a screen and bubble server or within the server.
    /// </summary>
    public abstract class Message
    {
        /// <summary>
        /// Identifies the type of the message.
        /// </summary>
        public MessageType Type
        {
            get { return m_type; }
        }
        
        public Message(MessageType type)
        {
            m_type = type;
        }
        
        /// <summary>
        /// Convert the message to a string that can be sent on the network.
        /// </summary>
        public abstract string Format();
        
        private readonly MessageType m_type;
    }
    
    public class AddMessage : Message
    {   
        public int BubbleID
        {
            get { return this.m_bubbleID; }
            set { m_bubbleID = value; }
        }
        
        public AddMessage(int bubbleID) : base(MessageType.Add)
        {
            m_bubbleID = bubbleID;
        }
        
        public override string Format()
        {
            return String.Format("add {0}", m_bubbleID);
        }
        
        private int m_bubbleID;
    }
    
    public class ChangeScreenMessage : Message
    {
        public ScreenDirection Direction
        {
            get { return this.m_direction; }
        }
        
        public int BubbleID
        {
            get { return this.m_bubbleID; }
            set { m_bubbleID = value; }
        }
        
        public ChangeScreenMessage(int bubbleID, ScreenDirection direction)
            : base(MessageType.ChangeScreen)
        {
            m_bubbleID = bubbleID;
            m_direction = direction;
        }
        
        public override string Format()
        {
            return String.Format("change-screen {0} {1}", m_bubbleID, FormatDirection());
        }
        
        private int m_bubbleID;
        private ScreenDirection m_direction;
        
        private string FormatDirection()
        {
            switch(m_direction)
            {
            case ScreenDirection.Left:
                return "left";
            case ScreenDirection.Right:
                return "right";
            default:
                throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    public class ConnectedMessage : Message
    {   
        public Socket Connection
        {
            get { return this.m_socket; }
        }
        
        public ConnectedMessage(Socket socket) : base(MessageType.Connected)
        {
            m_socket = socket;
        }
        
        public override string Format()
        {
            return String.Format("connected");
        }
        
        private Socket m_socket;
    }
    
    public class DisconnectedMessage : Message
    {   
        public int ScreenID
        {
            get { return this.m_screenID; }
        }
        
        public DisconnectedMessage(int screenID) : base(MessageType.Disconnected)
        {
            m_screenID = screenID;
        }
        
        public override string Format()
        {
            return String.Format("disconnected {0}", m_screenID);
        }
        
        private int m_screenID;
    }
}