using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectInput.Input;
using System.Threading;

namespace KinectInput
{
    class Program
    {
        static void Main(string[] args)
        {
            KinectControllerInput ki = new KinectControllerInput();
            ki.Initialize();

            while (true)
            {
                Console.WriteLine("Hands: ({0,8}, {1,8}) and ({2,8}, {3,8})", ki.LeftHand.X, ki.LeftHand.Y, ki.RightHand.X, ki.RightHand.Y);
                Thread.Sleep(100);
            }
        }
    }
}
