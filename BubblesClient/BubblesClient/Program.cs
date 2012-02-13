using System;
using System.Net;

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

            if (args.Length > 0)
            {
                if (!IPAddress.TryParse(args[0], out serverAddress))
                {
                    Console.WriteLine("Invalid Server IP Address: {0}", args[0]);
                    return;
                }
            }
            if (args.Length > 1)
            {
                if (!Int32.TryParse(args[1], out serverPort))
                {
                    Console.WriteLine("Invalid Port: {0}", args[1]);
                    return;
                }
            }

            using (BubblesClientGame game = new BubblesClientGame(new ScreenManager(serverAddress, serverPort)))
            {
                game.Run();
            }
        }
    }
#endif
}

