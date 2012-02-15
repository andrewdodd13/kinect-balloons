using System;
using System.Collections.Generic;
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
        Disconnected,
        RefreshFeed,
        FeedUpdated
    }
    
    /// <summary>
    /// Message that can be sent between a screen and bubble server or within the server.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Identifies the type of the message.
        /// </summary>
        public MessageType Type
        {
            get { return m_type; }
        }

        /// <summary>
        /// Identifies the type of the message.
        /// </summary>
        public string TypeTag
        {
            get { return m_tag; }
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
        
        private readonly MessageType m_type;
        private readonly string m_tag;
        private object m_sender;
    }

    public class BalloonMessage : Message
    {
        public string BalloonID
        {
            get { return this.m_balloonID; }
        }

        public BalloonMessage(MessageType type, string tag, string balloonID)
            : base(type, tag)
        {
            m_balloonID = balloonID;
        }

        private string m_balloonID;
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
        public const string Tag = "new-balloon";

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

        public NewBalloonMessage(string balloonID, Direction direction, float y, Vector2D velocity)
            : base(MessageType.NewBalloon, Tag, balloonID)
        {
            m_direction = direction;
            m_y = y;
            m_velocity = velocity;
        }

        private Direction m_direction;
        private Vector2D m_velocity;
        private float m_y;
    }

    public class BalloonContentUpdateMessage : BalloonMessage
    {
        public const string Tag = "balloon-content-update";

        public BalloonType BalloonType
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

        public BalloonContentUpdateMessage(string balloonID, BalloonType type, string label, string content, string url)
            : base(MessageType.BalloonContentUpdate, Tag, balloonID)
        {
            m_type = type;
            m_label = label;
            m_content = content;
            m_url = url;
        }

        private BalloonType m_type;
        private string m_label;
        private string m_content;
        private string m_url;
    }
    #endregion

    #region Messages sent by the client
    public class ChangeScreenMessage : BalloonMessage
    {
        public const string Tag = "change-screen";

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

        public ChangeScreenMessage(string balloonID, Direction direction, float y, Vector2D velocity)
            : base(MessageType.ChangeScreen, Tag, balloonID)
        {
            m_direction = direction;
            m_y = y;
            m_velocity = velocity;
        }
        
        private Direction m_direction;
        private Vector2D m_velocity;
        private float m_y;
    }

    public class GetBalloonContentMessage : BalloonMessage
    {
        public const string Tag = "get-balloon-content";

        public GetBalloonContentMessage(string balloonID)
            : base(MessageType.GetBalloonContent, Tag, balloonID)
        {
        }
    }

    public class GetBalloonDecorationMessage : BalloonMessage
    {
        public const string Tag = "get-balloon-decoration";

        public GetBalloonDecorationMessage(string balloonID)
            : base(MessageType.GetBalloonDecoration, Tag, balloonID)
        {
        }
    }
    #endregion

    #region Messages sent by both
    public class PopBalloonMessage : BalloonMessage
    {
        public const string Tag = "pop-balloon";

        public PopBalloonMessage(string balloonID)
            : base(MessageType.PopBalloon, Tag, balloonID)
        {
        }
    }

    public class BalloonDecorationUpdateMessage : BalloonMessage
    {
        public const string Tag = "balloon-decoration-update";

        public OverlayType OverlayType
        {
            get { return this.m_overlayType; }
        }

        public Colour BackgroundColor
        {
            get { return this.m_bgColor; }
        }

        public BalloonDecorationUpdateMessage(string balloonID, OverlayType overlayType, Colour bgColor)
            : base(MessageType.BalloonDecorationUpdate, Tag, balloonID)
        {
            m_overlayType = overlayType;
            m_bgColor = bgColor;
        }

        private OverlayType m_overlayType;
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

        public DisconnectedMessage() : base(MessageType.Disconnected, Tag)
        {
        }
    }
    
    public class FeedUpdatedMessage : Message
    {
        public static readonly string Tag = "feed-updated";

        public List<FeedContent> FeedItems
        {
            get { return this.m_items; }
        }
        
        public FeedUpdatedMessage(List<FeedContent> items) : base(MessageType.FeedUpdated, Tag)
        {
            m_items = items;
        }
        
        private List<FeedContent> m_items;
    }
    
    #endregion
}