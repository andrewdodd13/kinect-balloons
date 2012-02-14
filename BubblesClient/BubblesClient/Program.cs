using System;
using System.Net;
using BubblesClient.Input.Controllers;
using BubblesClient.Input.Controllers.Kinect;
using BubblesClient.Input.Controllers.Mouse;

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
            IPAddress serverAddress = IPAddress.Loopback;
            int serverPort = 4000;
            IInputController controller = null;

            if (args.Length > 0)
            {
                if (args[0] == "kinect")
                {
                    controller = new KinectControllerInput();
                }
            }

            if (controller == null)
            {
                controller = new MouseInput();
            }

            if (args.Length > 1)
            {
                if (!IPAddress.TryParse(args[1], out serverAddress))
                {
                    Console.WriteLine("Invalid Server IP Address: {0}", args[1]);
                    return;
                }
            }
            if (args.Length > 2)
            {
                if (!Int32.TryParse(args[2], out serverPort))
                {
                    Console.WriteLine("Invalid Port: {0}", args[2]);
                    return;
                }
            }

            using (BubblesClientGame game = new BubblesClientGame(new ScreenManager(serverAddress, serverPort), controller))
            {
                game.Run();
            }
        }
    }
#endif
}

