using Balloons.Messaging.Model;
using BubblesClient.Input.Kinect;
using BubblesClient.Input.Mouse;
using BubblesClient.Network;
using BubblesClient.Input;

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
            if (args.Length > 1)
            {
                configPath = args[1];
            }
            // If this path doesn't exist, the config file will be created with default values
            Configuration.Load(configPath);

            // Initialise the input controller
            IInputManager controller = null;
            switch (Configuration.InputType)
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
            using (NetworkManager screen = new NetworkManager(Configuration.RemoteIPAddress, Configuration.RemotePort))
            using (BalloonClient game = new BalloonClient(screen, controller))
            {
                game.Run();
            }
        }
    }
#endif
}

