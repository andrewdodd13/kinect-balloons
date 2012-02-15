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
    /// List of possible serializers.
    /// </summary>
    public enum SerializerType
    {
        Binary,
        JSON
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
        /// <summary>
        /// Which serializer should be used to send messages over the network.
        /// </summary>
        public static SerializerType SerializerType = SerializerType.Binary;
        /// <summary>
        /// Whether to log or not the messages sent and received from the network.
        /// </summary>
        public static bool LogNetworkMessages = false;
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
                JObject settings = LoadConfigFile(ConfigPath);
                LoadValues(settings);
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
            if(LogToFile && !String.IsNullOrWhiteSpace(LogFilePath))
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
            JObject settings = new JObject();

            // common settings
            StoreValue(settings, "LogToConsole", LogToConsole);
            StoreValue(settings, "LogToFile", LogToFile);
            StoreValue(settings, "LogFilePath", LogFilePath);
            StoreValue(settings, "SerializerType", SerializerType);
            StoreValue(settings, "LogNetworkMessages", LogNetworkMessages);

            // client settings
            StoreValue(settings, "InputType", InputType);
            StoreValue(settings, "FullScreen", FullScreen);
            StoreValue(settings, "ScreenWidth", ScreenWidth);
            StoreValue(settings, "ScreenHeight", ScreenHeight);
            StoreValue(settings, "MessageDisplayTime", MessageDisplayTime);
            StoreValue(settings, "RemoteIPAddress", RemoteIPAddress);
            StoreValue(settings, "RemotePort", RemotePort);

            // server settings
            StoreValue(settings, "LocalIPAddress", LocalIPAddress);
            StoreValue(settings, "LocalPort", LocalPort);
            StoreValue(settings, "FeedURL", FeedURL);
            StoreValue(settings, "FeedTimeout", FeedTimeout);
            StoreValue(settings, "MinBalloonsPerScreen", MinBalloonsPerScreen);
            StoreValue(settings, "MaxBalloonsPerScreen", MaxBalloonsPerScreen);

            StoreValue(settings, "VelocityLeft", VelocityLeft);
            StoreValue(settings, "VelocityRight", VelocityRight);

            // write the settings to the configuration file, as JSON
            string jsonText = settings.ToString();
            try
            {
                File.WriteAllText(ConfigPath, jsonText);
                return true;
            }
            catch(Exception e)
            {
                LogError("Error when writing configuration file", e);
                return false;
            }
        }

        private static JObject LoadConfigFile(string path)
        {
            string jsonText = null;
            try
            {
                jsonText = File.ReadAllText(path);
                return JObject.Parse(jsonText);
            }
            catch(Exception e)
            {
                LogError("Error when reading configuration file", e);
                return null;
            }
        }

        private static void LoadValues(JObject settings)
        {
            if(settings == null)
            {
                return;
            }

            // common settings
            LoadValue(settings, "LogToConsole", out LogToConsole);
            LoadValue(settings, "LogToFile", out LogToFile);
            LoadValue(settings, "LogFilePath", out LogFilePath);
            LoadValue(settings, "SerializerType", out SerializerType);
            LoadValue(settings, "LogNetworkMessages", out LogNetworkMessages);

            // client settings
            LoadValue(settings, "InputType", out InputType);
            LoadValue(settings, "FullScreen", out FullScreen);
            LoadValue(settings, "ScreenWidth", out ScreenWidth);
            LoadValue(settings, "ScreenHeight", out ScreenHeight);
            LoadValue(settings, "MessageDisplayTime", out MessageDisplayTime);
            LoadValue(settings, "RemoteIPAddress", out RemoteIPAddress);
            LoadValue(settings, "RemotePort", out RemotePort);

            // server settings
            LoadValue(settings, "LocalIPAddress", out LocalIPAddress);
            LoadValue(settings, "LocalPort", out LocalPort);
            LoadValue(settings, "FeedURL", out FeedURL);
            LoadValue(settings, "FeedTimeout", out FeedTimeout);
            LoadValue(settings, "MinBalloonsPerScreen", out MinBalloonsPerScreen);
            LoadValue(settings, "MaxBalloonsPerScreen", out MaxBalloonsPerScreen);

            LoadValue(settings, "VelocityLeft", out VelocityLeft);
            LoadValue(settings, "VelocityRight", out VelocityRight);
        }

        private static bool LoadValue<T>(JObject settings, string key, out T val)
        {
            if(typeof(T).IsEnum)
            {
                string text;
                val = default(T);
                if(LoadValueInternal(settings, key, out text))
                {
                    try
                    {
                        val = (T)Enum.Parse(typeof(T), text);
                        return true;
                    }
                    catch(Exception e)
                    {
                    }
                }
                return false;
            }
            else
            {
                return LoadValueInternal(settings, key, out val);
            }
        }

        private static bool LoadValueInternal<T>(JObject settings, string key, out T val)
        {
            JToken jVal = null;
            val = default(T);
            if(settings.TryGetValue(key, out jVal))
            {
                try
                {
                    val = jVal.ToObject<T>();
                    return true;
                }
                catch(Exception e)
                {
                    LogError("Invalid type for the configuration value", e);
                }
            }
            return false;
        }

        private static bool LoadValue(JObject settings, string key, out Vector2D val)
        {
            float x, y;
            val = new Vector2D(0.0f, 0.0f);
            if(LoadValue(settings, key + "X", out x) && LoadValue(settings, key + "Y", out y))
            {
                val = new Vector2D(x, y);
                return true;
            }
            return false;
        }

        private static bool LoadValue(JObject settings, string key, out IPAddress val)
        {
            string text;
            val = null;
            if(LoadValue(settings, key, out text))
            {
                return IPAddress.TryParse(text, out val);
            }
            return false;
        }

        private static void StoreValue<T>(JObject settings, string key, T val)
        {
            if(typeof(T).IsEnum)
            {
                settings[key] = JValue.CreateString(val.ToString());
            }
            else
            {
                settings[key] = JValue.FromObject(val);
            }
        }

        private static void StoreValue(JObject settings, string key, string val)
        {
            settings[key] = (val == null) ? null : JValue.FromObject(val);
        }

        private static void StoreValue(JObject settings, string key, Vector2D val)
        {
            StoreValue(settings, key + "X", val.X);
            StoreValue(settings, key + "Y", val.Y);
        }

        private static void StoreValue(JObject settings, string key, IPAddress val)
        {
            settings[key] = (val == null) ? null : JValue.CreateString(val.ToString());
        }

        private static void LogError(string message, Exception e)
        {
            // TODO: handle logging before the configuration is loaded
            Console.WriteLine(String.Format("{0}: {1}", message, e));
        }
        #endregion
    }
}
