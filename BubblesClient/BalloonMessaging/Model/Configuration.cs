using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace Balloons.Messaging.Model
{
    /// <summary>
    /// List of possible input types.
    /// </summary>
    public enum InputType
    {
        Mouse,
        Kinect
    }

    /// <summary>
    /// Contains application-wide configuration settings and constants.
    /// </summary>
    public class Configuration
    {
        #region Common settings
        /// <summary>
        /// Where to find the configuration file.
        /// </summary>
        public static string ConfigPath = "balloons.js";
        /// <summary>
        /// Are application events and errors (from Trace) written to the console?
        /// </summary>
        public static bool LogToConsole = true;
        /// <summary>
        /// Is logging application events and errors (from Trace) to a file enabled?
        /// </summary>
        public static bool LogToFile = false;
        /// <summary>
        /// Which file application events and errors (from Trace) should be logged to.
        /// </summary>
        public static string LogFile = null;
        /// <summary>
        /// Which input type should be used for manipulating balloons.
        /// </summary>
        public static InputType InputType = InputType.Mouse;
        #endregion

        #region Client settings
        /// <summary>
        /// Should the client be rendered in full screen
        /// </summary>
        public static bool FullScreen = false;
        /// <summary>
        /// Screen width (0 for XNA default).
        /// </summary>
        public static int ScreenWidth = 1366;
        /// <summary>
        /// Screen height (0 for XNA default).
        /// </summary>
        public static int ScreenHeight = 768;
        /// <summary>
        /// How long (in ms) the content box that appears when a balloon is popped should be shown.
        /// </summary>
        public static int MessageDisplayTime = 30 * 1000;      // TODO TimeSpan?
        /// <summary>
        /// IP address of the server to connect to.
        /// </summary>
        public static IPAddress RemoteIPAddress = IPAddress.Loopback;
        /// <summary>
        /// IP port of the server to connect to.
        /// </summary>
        public static int RemotePort = 4000;
        #endregion

        #region Server settings
        /// <summary>
        /// IP address the server should listen on.
        /// </summary>
        public static IPAddress LocalIPAddress = IPAddress.Any;
        /// <summary>
        /// IP port the server should listen on.
        /// </summary>
        public static int LocalPort = 4000;
        /// <summary>
        /// URL to the feed on the web server, including a placeholder ({0}) for the number of items to pull.
        /// </summary>
        public static string FeedURL = "http://www.macs.hw.ac.uk/~cgw4/balloons/index.php/api/getFeed/{0}";
        /// <summary>
        /// How long (in seconds) should the server wait between updated of the feed.
        /// </summary>
        public static int FeedTimeout = 1000 * 60 * 1;    // TODO TimeSpan?
        /// <summary>
        /// Minimum number of balloons that triggers updating the feed before it times out.
        /// </summary>
        public static int MinBalloonPerScreen = 1;
        /// <summary>
        /// Maximum number of balloons per screen (not enforced per-screen but globally).
        /// </summary>
        public static int MaxBalloonsPerScreen = 5;
        /// <summary>
        /// When a screen disconnects, its balloon randomly move to either the left or right edge of the screen.
        /// This is the velocity given to a balloon which moves to the right side of the screen.
        /// </summary>
        public static Vector2D VelocityLeft = new Vector2D(-10.0f, 0.0f);
        /// <summary>
        /// When a screen disconnects, its balloon randomly move to either the left or right edge of the screen.
        /// This is the velocity given to a balloon which moves to the left side of the screen.
        /// </summary
        public static Vector2D VelocityRight = new Vector2D(10.0f, 0.0f);
        #endregion

        #region Implementation
        /// <summary>
        /// Try to load the configuration file, default values are kept otherwise.
        /// Setup logging if needed.
        /// </summary>
        /// <returns> False on error. </returns>
        public static bool Load()
        {
            if(LogToConsole)
            {
                Debug.Listeners.Add(new ConsoleTraceListener());
            }
            return false;
        }
        #endregion
    }
}
