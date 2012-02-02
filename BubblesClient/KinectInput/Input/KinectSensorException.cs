using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectInput.Input
{
    class KinectSensorException : Exception
    {
        private string _message;
        public string Message { get { return _message; } }

        private string _moreInfo;
        public string MoreInfo { get { return _moreInfo; } }

        private Uri _moreInfoUri;
        public Uri MoreInfoUri { get { return _moreInfoUri; } }

        public KinectSensorException(string message, string moreInfo, Uri moreInfoUri)
        {
            _message = message;
            _moreInfo = moreInfo;
            _moreInfoUri = moreInfoUri;
        }
    }
}
