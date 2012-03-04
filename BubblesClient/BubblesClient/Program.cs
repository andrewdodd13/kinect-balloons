using Balloons.Messaging.Model;
using BubblesClient.Input.Controllers;
using BubblesClient.Input.Controllers.Kinect;
using BubblesClient.Input.Controllers.Mouse;
using BubblesClient.Network;

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
            IInputController controller = null;
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
            using (BubblesClientGame game = new BubblesClientGame(screen, controller))
            {
                game.Run();
            }
        }
    }
#endif
}

