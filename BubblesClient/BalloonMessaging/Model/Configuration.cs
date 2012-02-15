using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

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
        public static string ConfigPath = "Balloons.conf";
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
        public static string LogFilePath = null;
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
        public static int MinBalloonsPerScreen = 1;
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
            if(File.Exists(ConfigPath))
            {

            }
            else
            {
                // dump the default settings to a file so that they can be easily edited
                Save(ConfigPath);
            }

            // Setup logging
            if(LogToConsole)
            {
                Trace.Listeners.Add(new ConsoleTraceListener());
                Trace.AutoFlush = true;
            }
            if(LogToFile)
            {
                Trace.Listeners.Add(new TextWriterTraceListener(LogFilePath));
            }
            return false;
        }

        /// <summary>
        /// Save the current configuration to a file.
        /// </summary>
        public static bool Save(string path)
        {
            //Dictionary<string, object> settings = new Dictionary<string, object>();
            JObject settings = new JObject();

            // common settings
            settings["LogToConsole"] = JValue.FromObject(LogToConsole);
            settings["LogToFile"] = JValue.FromObject(LogToFile);
            settings["LogFilePath"] = (LogFilePath == null) ? null : JValue.FromObject(LogFilePath);

            // client settings
            settings["InputType"] = JValue.FromObject(InputType.ToString());
            settings["FullScreen"] = JValue.FromObject(FullScreen);
            settings["ScreenWidth"] = JValue.FromObject(ScreenWidth);
            settings["ScreenHeight"] = JValue.FromObject(ScreenHeight);
            settings["MessageDisplayTime"] = JValue.FromObject(MessageDisplayTime);
            settings["RemoteIPAddress"] = JValue.FromObject(RemoteIPAddress.ToString());
            settings["RemotePort"] = JValue.FromObject(RemotePort);

            // server settings
            settings["LocalIPAddress"] = JValue.FromObject(LocalIPAddress.ToString());
            settings["LocalPort"] = JValue.FromObject(LocalPort);
            settings["FeedURL"] = (FeedURL == null) ? null : JValue.FromObject(FeedURL);
            settings["FeedTimeout"] = JValue.FromObject(FeedTimeout);
            settings["MinBalloonsPerScreen"] = JValue.FromObject(MinBalloonsPerScreen);
            settings["MaxBalloonsPerScreen"] = JValue.FromObject(MaxBalloonsPerScreen);
            settings["VelocityLeftX"] = JValue.FromObject(VelocityLeft.X);
            settings["VelocityLeftY"] = JValue.FromObject(VelocityLeft.Y);
            settings["VelocityRightX"] = JValue.FromObject(VelocityRight.X);
            settings["VelocityRightY"] = JValue.FromObject(VelocityRight.Y);

            // write the settings to the configuration file, as JSON
            string jsonText = settings.ToString();
            try
            {
                File.WriteAllText(ConfigPath, jsonText);
                return true;
            }
            catch(Exception e)
            {
                Trace.WriteLine(String.Format("Error when writing configuration file: {0}", e));
                return false;
            }
        }
        #endregion
    }
}
