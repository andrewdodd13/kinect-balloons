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
        /// <summary>
        /// How big the dead zone is relative to the size of a balloon. Behaviour 
        /// is undefined if this is less than or equal to 1.0f.
        /// </summary>
        public static float BalloonDeadzoneMultiplier = 1.1f;
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
        /// How long (in ms) the balloon pop animation should be shown.
        /// </summary>
        public static int PopAnimationTime = 2500;
        /// <summary>
        /// Whether the balloon pop animation is enabled or disabled (in that case the pop texture is static).
        /// </summary>
        public static bool PopAnimationEnabled = true;
        /// <summary>
        /// Alpha parameter of the balloon pop animation. Controls how much smaller/bigger the texture becomes.
        /// </summary>
        public static float PopAnimationAlpha = 0.3f;
        /// <summary>
        /// Beta parameter of the balloon pop animation. Controls how fast the size of the texture changes.
        /// </summary>
        public static float PopAnimationBeta = 5.0f;
        /// <summary>
        /// Scale parameter of the balloon pop animation.
        /// </summary>
        public static float PopAnimationScale = 2.0f;
        /// <summary>
        /// Use HTML for rendering content boxes
        /// </summary>
        public static bool UseHtmlRendering = true;
        /// <summary>
        /// IP address of the server to connect to.
        /// </summary>
        public static IPAddress RemoteIPAddress = IPAddress.Loopback;
        /// <summary>
        /// IP port of the server to connect to.
        /// </summary>
        public static int RemotePort = 4000;
        /// <summary>
        /// Controls if hands from different users can trigger a clap
        /// </summary>
        public static bool EnableHighFive = false;
        /// <summary>
        /// The minimum speed a hand must be moving to be valid to clap
        /// Units are meters/sec
        /// </summary>
        public static float KinectMovementThreshold = 2;
        /// <summary>
        /// The minimum range a hand must be from a balloon before it is valid to burt it
        /// Units are meters
        /// </summary>
        public static float KinectMaxHandRange = 2;
        /// <summary>
        /// The minimum angle of attack that a hand must hit a balloon at to be valid for bursting
        /// Also used as tolerance angle for if hands are moving towards each other
        /// Unit is cos(angle) [Range 0 to 1]
        /// </summary>
        public static double KinectMinAttackAngle = 0.4;
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
        public static bool Load(string configPath)
        {
            if(File.Exists(configPath))
            {
                JObject settings = LoadConfigFile(configPath);
                LoadValues(settings);
            }
            else
            {
                // dump the default settings to a file so that they can be easily edited
                Save(configPath);
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
            StoreValue(settings, "BalloonDeadzoneMultiplier", BalloonDeadzoneMultiplier);

            // client settings
            StoreValue(settings, "InputType", InputType);
            StoreValue(settings, "FullScreen", FullScreen);
            StoreValue(settings, "ScreenWidth", ScreenWidth);
            StoreValue(settings, "ScreenHeight", ScreenHeight);
            StoreValue(settings, "MessageDisplayTime", MessageDisplayTime);
            StoreValue(settings, "PopAnimationTime", PopAnimationTime);
            StoreValue(settings, "PopAnimationEnabled", PopAnimationEnabled);
            StoreValue(settings, "PopAnimationAlpha", PopAnimationAlpha);
            StoreValue(settings, "PopAnimationBeta", PopAnimationBeta);
            StoreValue(settings, "PopAnimationScale", PopAnimationScale);
            StoreValue(settings, "UseHtmlRendering", UseHtmlRendering);
            StoreValue(settings, "RemoteIPAddress", RemoteIPAddress);
            StoreValue(settings, "RemotePort", RemotePort);
            StoreValue(settings, "EnableHighFive", EnableHighFive);
            StoreValue(settings, "KinectMovementThreshold", KinectMovementThreshold);
            StoreValue(settings, "KinectMaxHandRange", KinectMaxHandRange);
            StoreValue(settings, "KinectMinAttackAngle", KinectMinAttackAngle);

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
                File.WriteAllText(path, jsonText);
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
            LoadValue(settings, "LogToConsole", ref LogToConsole);
            LoadValue(settings, "LogToFile", ref LogToFile);
            LoadValue(settings, "LogFilePath", ref LogFilePath);
            LoadValue(settings, "SerializerType", ref SerializerType);
            LoadValue(settings, "LogNetworkMessages", ref LogNetworkMessages);
            LoadValue(settings, "BalloonDeadzoneMultiplier", ref BalloonDeadzoneMultiplier);

            // client settings
            LoadValue(settings, "InputType", ref InputType);
            LoadValue(settings, "FullScreen", ref FullScreen);
            LoadValue(settings, "ScreenWidth", ref ScreenWidth);
            LoadValue(settings, "ScreenHeight", ref ScreenHeight);
            LoadValue(settings, "MessageDisplayTime", ref MessageDisplayTime);
            LoadValue(settings, "PopAnimationTime", ref PopAnimationTime);
            LoadValue(settings, "PopAnimationEnabled", ref PopAnimationEnabled);
            LoadValue(settings, "PopAnimationAlpha", ref PopAnimationAlpha);
            LoadValue(settings, "PopAnimationBeta", ref PopAnimationBeta);
            LoadValue(settings, "PopAnimationScale", ref PopAnimationScale);
            LoadValue(settings, "UseHtmlRendering", ref UseHtmlRendering);
            LoadValue(settings, "RemoteIPAddress", ref RemoteIPAddress);
            LoadValue(settings, "RemotePort", ref RemotePort);
            LoadValue(settings, "EnableHighFive", ref EnableHighFive);
            LoadValue(settings, "KinectMovementThreshold", ref KinectMovementThreshold);
            LoadValue(settings, "KinectMaxHandRange", ref KinectMaxHandRange);
            LoadValue(settings, "KinectMinAttackAngle", ref KinectMinAttackAngle);

            // server settings
            LoadValue(settings, "LocalIPAddress", ref LocalIPAddress);
            LoadValue(settings, "LocalPort", ref LocalPort);
            LoadValue(settings, "FeedURL", ref FeedURL);
            LoadValue(settings, "FeedTimeout", ref FeedTimeout);
            LoadValue(settings, "MinBalloonsPerScreen", ref MinBalloonsPerScreen);
            LoadValue(settings, "MaxBalloonsPerScreen", ref MaxBalloonsPerScreen);

            LoadValue(settings, "VelocityLeft", ref VelocityLeft);
            LoadValue(settings, "VelocityRight", ref VelocityRight);
        }

        private static bool LoadValue<T>(JObject settings, string key, ref T val)
        {
            if(typeof(T).IsEnum)
            {
                string text = null;
                if(LoadValueInternal(settings, key, ref text))
                {
                    try
                    {
                        val = (T)Enum.Parse(typeof(T), text);
                        return true;
                    }
                    catch(Exception)
                    {
                    }
                }
                return false;
            }
            else
            {
                return LoadValueInternal(settings, key, ref val);
            }
        }

        private static bool LoadValueInternal<T>(JObject settings, string key, ref T val)
        {
            JToken jVal = null;
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

        private static bool LoadValue(JObject settings, string key, ref Vector2D val)
        {
            float x = 0.0f, y = 0.0f;
            if(LoadValue(settings, key + "X", ref x) && LoadValue(settings, key + "Y", ref y))
            {
                val = new Vector2D(x, y);
                return true;
            }
            return false;
        }

        private static bool LoadValue(JObject settings, string key, ref IPAddress val)
        {
            string text = null;
            if(LoadValue(settings, key, ref text))
            {
                IPAddress address;
                if(IPAddress.TryParse(text, out address))
                {
                    val = address;
                    return true;
                }
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
