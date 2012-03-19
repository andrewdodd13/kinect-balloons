using System;

namespace BubblesClient.Input.Kinect
{
    /// <summary>
    /// Thrown when an exception occurs during polling of the Kinect; usually to indicate that the device has malfunctioned in some way.
    /// </summary>
    public class KinectSensorException : Exception
    {
        public string ErrorMessage { get; private set; }
        public string MoreInfo { get ; private set; }
        public Uri MoreInfoUri { get; private set; }

        public KinectSensorException(string message, string moreInfo, Uri moreInfoUri)
        {
            ErrorMessage = message;
            MoreInfo = moreInfo;
            MoreInfoUri = moreInfoUri;
        }
    }
}
