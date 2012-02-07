using System;
using System.Net.Sockets;

namespace Balloons.Messaging.Model
{
    /// <summary>
    /// List of all message types.
    /// </summary>
    public enum MessageType
    {
        // Messages sent by the server
        NewBalloon,
        BalloonContentUpdate,

        // Messages sent by the client
        ChangeScreen,
        GetBalloonContent,
        GetBalloonDecoration,

        // Messages sent by both
        PopBalloon,
        BalloonDecorationUpdate,

        // Internal messages
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

        /// <summary>
        /// Object that sent this message.
        /// </summary>
        public object Sender
        {
            get { return m_sender; }
            set { m_sender = value; }
        }
        
        public Message(MessageType type, string tag)
        {
            m_type = type;
            m_tag = tag;
        }
        
        /// <summary>
        /// Convert the message to a string that can be sent on the network.
        /// </summary>
        public virtual string Format()
        {
            return String.Format("{0} {1}", m_tag, FormatContent());
        }

        protected virtual string FormatContent()
        {
            return "";
        }
        
        private readonly MessageType m_type;
        private readonly string m_tag;
        private object m_sender;
    }

    public class BalloonMessage : Message
    {
        public int BalloonID
        {
            get { return this.m_balloonID; }
        }

        public BalloonMessage(MessageType type, string tag, int balloonID) : base(type, tag)
        {
            m_balloonID = balloonID;
        }

        protected override string FormatContent()
        {
            return m_balloonID.ToString();
        }

        private int m_balloonID;
    }

    public class MessageEventArgs : EventArgs
    {
        public Message Message
        {
            get { return m_message; }
            set { m_message = value; }
        }

        public MessageEventArgs(Message message)
        {
            m_message = message;
        }

        private Message m_message;
    }

    #region Messages sent by the server
    public class NewBalloonMessage : BalloonMessage
    {
        public static readonly string Tag = "new-ballooon";

        public Direction Direction
        {
            get { return this.m_direction; }
        }

        public float Y
        {
            get { return this.m_y; }
        }

        public Vector2D Velocity
        {
            get { return this.m_velocity; }
        }

        public NewBalloonMessage(int balloonID, Direction direction, float y, Vector2D velocity)
            : base(MessageType.NewBalloon, Tag, balloonID)
        {
            m_direction = direction;
            m_y = y;
            m_velocity = velocity;
        }

        protected override string FormatContent()
        {
            return String.Format("{0} {1} {2} {3} {4}",
                BalloonID, Balloon.FormatDirection(m_direction), m_y, m_velocity.X, m_velocity.Y);
        }
        
        private Direction m_direction;
        private Vector2D m_velocity;
        private float m_y;
    }

    public class BalloonContentUpdateMessage : BalloonMessage
    {
        public static readonly string Tag = "balloon-content-update";

        public int BalloonType
        {
            get { return this.m_type; }
        }

        public string Label
        {
            get { return this.m_label; }
        }

        public string Content
        {
            get { return this.m_content; }
        }

        public string Url
        {
            get { return this.m_url; }
        }

        public BalloonContentUpdateMessage(int balloonID, int type, string label, string content, string url)
            : base(MessageType.BalloonContentUpdate, Tag, balloonID)
        {
            m_type = type;
            m_label = label;
            m_content = content;
            m_url = url;
        }

        protected override string FormatContent()
        {
            return String.Format("{0} {1} {2} {3}",
                BalloonID, m_type, m_label, m_content, m_url);
        }

        private int m_type;
        private string m_label;
        private string m_content;
        private string m_url;
    }
    #endregion

    #region Messages sent by the client
    public class ChangeScreenMessage : BalloonMessage
    {
        public static readonly string Tag = "change-screen";

        public Direction Direction
        {
            get { return this.m_direction; }
        }

        public float Y
        {
            get { return this.m_y; }
        }

        public Vector2D Velocity
        {
            get { return this.m_velocity; }
        }

        public ChangeScreenMessage(int balloonID, Direction direction, float y, Vector2D velocity)
            : base(MessageType.ChangeScreen, Tag, balloonID)
        {
            m_direction = direction;
            m_y = y;
            m_velocity = velocity;
        }
        
        protected override string FormatContent()
        {
            return String.Format("{0} {1} {2} {3} {4}",
                BalloonID, Balloon.FormatDirection(m_direction), m_y, m_velocity.X, m_velocity.Y);
        }
        
        private Direction m_direction;
        private Vector2D m_velocity;
        private float m_y;
    }

    public class GetBalloonContentMessage : BalloonMessage
    {
        public static readonly string Tag = "get-balloon-content";

        public GetBalloonContentMessage(int balloonID)
            : base(MessageType.GetBalloonContent, Tag, balloonID)
        {
        }
    }

    public class GetBalloonDecorationMessage : BalloonMessage
    {
        public static readonly string Tag = "get-balloon-decoration";

        public GetBalloonDecorationMessage(int balloonID)
            : base(MessageType.GetBalloonDecoration, Tag, balloonID)
        {
        }
    }
    #endregion

    #region Messages sent by both
    public class PopBalloonMessage : BalloonMessage
    {
        public static readonly string Tag = "pop-balloon";

        public PopBalloonMessage(int balloonID)
            : base(MessageType.PopBalloon, Tag, balloonID)
        {
        }
    }

    public class BalloonDecorationUpdateMessage : BalloonMessage
    {
        public static readonly string Tag = "balloon-decoration-update";

        public int OverlayType
        {
            get { return this.m_overlayType; }
        }

        public Colour BackgroundColor
        {
            get { return this.m_bgColor; }
        }

        public BalloonDecorationUpdateMessage(int balloonID, int overlayType, Colour bgColor)
            : base(MessageType.BalloonDecorationUpdate, Tag, balloonID)
        {
            m_overlayType = overlayType;
            m_bgColor = bgColor;
        }

        protected override string FormatContent()
        {
            return String.Format("{0} {1} {2} {3}",
                BalloonID, m_overlayType, m_bgColor.Red, m_bgColor.Green, m_bgColor.Blue);
        }

        private int m_overlayType;
        private Colour m_bgColor;
    }
    #endregion

    #region Internal messages
    public class ConnectedMessage : Message
    {
        public static readonly string Tag = "connected";

        public Socket Connection
        {
            get { return this.m_socket; }
        }
        
        public ConnectedMessage(Socket socket) : base(MessageType.Connected, Tag)
        {
            m_socket = socket;
        }
        
        private Socket m_socket;
    }
    
    public class DisconnectedMessage : Message
    {
        public static readonly string Tag = "disconnected";

        public int ScreenID
        {
            get { return this.m_screenID; }
        }

        public DisconnectedMessage(int screenID) : base(MessageType.Disconnected, Tag)
        {
            m_screenID = screenID;
        }
        
        protected override string FormatContent()
        {
            return m_screenID.ToString();
        }
        
        private int m_screenID;
    }
    
    #endregion
}