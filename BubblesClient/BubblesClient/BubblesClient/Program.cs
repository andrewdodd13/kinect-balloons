using System;

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
            using (BubblesClientGame game = new BubblesClientGame())
            {
                game.Run();
            }
        }
    }
#endif
}

