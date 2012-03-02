using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using BubblesClient.Input.Controllers;
using BubblesClient.Input.Controllers.Kinect;
using BubblesClient.Input.Controllers.Mouse;
using Balloons.Messaging.Model;

namespace BubblesClient
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // Load the configuration file
            string configPath = "BalloonClient.conf";
            if(args.Length > 1)
            {
                configPath = args[1];
            }
            // If this path doesn't exist, the config file will be created with default values
            Configuration.Load(configPath);
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            // Initialise the input controller
            IInputController controller = null;
            switch(Configuration.InputType)
            {
            default:
            case InputType.Mouse:
                controller = new MouseInput();
                break;
            case InputType.Kinect:
                controller = new KinectControllerInput();
                break;
            }

            // Run the game
            using(ScreenManager screen = new ScreenManager(Configuration.RemoteIPAddress, Configuration.RemotePort))
            using(BubblesClientGame game = new BubblesClientGame(screen, controller))
            {
                game.Run();
            }
        }

        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                Trace.WriteLine(String.Format("Unhandled exception: {0}", ex.Message));
                Trace.WriteLine(ex.StackTrace);
            }
        }
    }
#endif
}

