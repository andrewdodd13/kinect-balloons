using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using Microsoft.Xna.Framework;

namespace BubblesClient.Input.Controllers.Kinect
{
    public class KinectControllerInput : IInputController
    {
        private float _scaleFactorX, _scaleFactorY;
        private Vector2 halfScreenSize;

        private KinectSensor _sensor;
        private bool _sensorConflict = false;
        private Skeleton[] _skeletonData = null;

        private Dictionary<Skeleton, Hand[]> _handPositions = new Dictionary<Skeleton, Hand[]>();

        /// <summary>
        /// Initialises the Kinect system and causes it to begin polling.
        /// </summary>
        /// <param name="screenSize">Dimensions of the screen, used to rationalise the values of the Kinect sensor</param>
        public void Initialize(Vector2 screenSize)
        {
            _scaleFactorX = 1.5f;
            _scaleFactorY = 2;

            halfScreenSize = screenSize / 2;

            KinectSensor.KinectSensors.StatusChanged += this.KinectSensorsStatusChanged;
            if (!this.DiscoverSensor())
            {
                this.UpdateStatus(KinectStatus.Disconnected);
            }
        }

        public Hand[] GetHandPositions()
        {
            Hand[] returnValue;

            // Lock the hand positions map so we don't get concurrency issues
            lock (_handPositions)
            {
                returnValue = new Hand[_handPositions.Count * 2];
                int i = 0;
                foreach (Hand[] hands in _handPositions.Values)
                {
                    returnValue[i] = hands[0];
                    returnValue[i + 1] = hands[1];
                    i += 2;
                }
            }

            return returnValue;
        }


        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    if ((_skeletonData == null) || (_skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        _skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(_skeletonData);

                    lock (_handPositions)
                    {
                        List<Skeleton> matched = new List<Skeleton>();
                        foreach (Skeleton skeleton in _skeletonData)
                        {
                            if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                            {
                                if (skeleton.Joints.Count > 0)
                                {
                                    // Check to see if we're already tracking this skeleton
                                    Hand[] hands;
                                    if (!_handPositions.ContainsKey(skeleton))
                                    {
                                        // If not, add new hand objects to the list
                                        _handPositions.Add(skeleton, new Hand[2] { new Hand(), new Hand() });
                                    }

                                    // Set the hands positions
                                    hands = _handPositions[skeleton];
                                    SkeletonPoint leftHand = skeleton.Joints[JointType.HandLeft].Position;
                                    hands[0].Position = new Vector3(convertRawHandToScreen(leftHand), leftHand.Z);

                                    SkeletonPoint rightHand = skeleton.Joints[JointType.HandRight].Position;
                                    hands[1].Position = new Vector3(convertRawHandToScreen(rightHand), rightHand.Z);

                                    // Add this skeleton to the list of skeletons matched this frame
                                    matched.Add(skeleton);
                                }
                            }
                        }

                        // Make a new list with all the hands found this frame
                        Dictionary<Skeleton, Hand[]> newHands = new Dictionary<Skeleton, Hand[]>();
                        matched.ForEach(s => newHands.Add(s, _handPositions[s]));

                        _handPositions = newHands;
                    }
                }
            }
        }

        public bool ShouldClosePopup()
        {
            throw new NotImplementedException("Gotta close the popup!");
        }

        private Vector2 convertRawHandToScreen(SkeletonPoint raw)
        {
            return convertRawHandToScreen(new Vector2(raw.X, raw.Y));
        }

        private Vector2 convertRawHandToScreen(Vector2 raw)
        {
            raw.X *= _scaleFactorX;
            raw.Y *= _scaleFactorY;

            return new Vector2(halfScreenSize.X * raw.X + halfScreenSize.X, halfScreenSize.Y * -(raw.Y) + halfScreenSize.Y);
        }

        #region "Kinect Boilerplate Code"
        private bool DiscoverSensor()
        {
            foreach (KinectSensor sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    // Update the sensor status
                    _sensor = sensor;
                    this.UpdateStatus(sensor.Status);
                    break;
                }
            }

            return _sensor != null;
        }

        private void UpdateStatus(KinectStatus status)
        {
            string message = null;
            string moreInfo = null;
            Uri moreInfoUri = null;
            bool failed = true;

            switch (status)
            {
                case KinectStatus.Connected:
                    // If there's a sensor conflict, we wish to display all of the normal 
                    // states and statuses, with the exception of Connected.
                    if (_sensorConflict)
                    {
                        message = "This Kinect is being used by another application.";
                        moreInfo = "This application needs a Kinect for Windows sensor in order to function. However, another application is using the Kinect Sensor.";
                        moreInfoUri = new Uri("http://go.microsoft.com/fwlink/?LinkID=239812");
                    }
                    else
                    {
                        // If the kinect is connected then ok!
                        failed = false;
                    }

                    break;
                case KinectStatus.DeviceNotGenuine:
                    message = "This sensor is not genuine!";
                    moreInfo = "This application needs a genuine Kinect for Windows sensor in order to function. Please plug one into the PC.";
                    moreInfoUri = new Uri("http://go.microsoft.com/fwlink/?LinkID=239813");

                    break;
                case KinectStatus.DeviceNotSupported:
                    message = "Kinect for Xbox not supported.";
                    moreInfo = "This application needs a Kinect for Windows sensor in order to function. Please plug one into the PC.";
                    moreInfoUri = new Uri("http://go.microsoft.com/fwlink/?LinkID=239814");

                    break;
                case KinectStatus.Disconnected:
                    message = "Required";
                    moreInfo = "This application needs a Kinect for Windows sensor in order to function. Please plug one into the PC.";
                    moreInfoUri = new Uri("http://go.microsoft.com/fwlink/?LinkID=239815");

                    break;
                case KinectStatus.NotReady:
                case KinectStatus.Error:
                    message = "Oops, there is an error.";
                    moreInfo = "The Kinect Sensor is plugged in, however an error has occured. For steps to resolve, please click the \"Tell me more\" link.";
                    moreInfoUri = new Uri("http://go.microsoft.com/fwlink/?LinkID=239817");
                    break;
                case KinectStatus.Initializing:
                    message = "Initializing...";
                    moreInfo = null;
                    moreInfoUri = null;
                    break;
                case KinectStatus.InsufficientBandwidth:
                    message = "Too many USB devices! Please unplug one or more.";
                    moreInfo = "The Kinect Sensor needs the majority of the USB Bandwidth of a USB Controller. If other devices are in contention for that bandwidth, the Kinect Sensor may not be able to function.";
                    moreInfoUri = new Uri("http://go.microsoft.com/fwlink/?LinkID=239818");
                    break;
                case KinectStatus.NotPowered:
                    message = "Plug my power cord in!";
                    moreInfo = "The Kinect Sensor is plugged into the computer with its USB connection, but the power plug appears to be not powered.";
                    moreInfoUri = new Uri("http://go.microsoft.com/fwlink/?LinkID=239819");
                    break;
            }

            if (!failed)
            {
                InitialiseKinect();
            }
            else
            {
                throw new KinectSensorException(message, moreInfo, moreInfoUri);
            }
        }

        private void KinectSensorsStatusChanged(object sender, StatusChangedEventArgs e)
        {
            var status = e.Status;
            if (_sensor == e.Sensor)
            {
                this.UpdateStatus(status);
                if (e.Status == KinectStatus.Disconnected ||
                    e.Status == KinectStatus.NotPowered)
                {
                    // Fire a Kinect Disconnected event
                }
            }
        }

        private void AppConflictOccurred()
        {
            _sensorConflict = true;
            this.UpdateStatus(_sensor.Status);
        }

        private void InitialiseKinect()
        {
            try
            {
                _sensor.SkeletonFrameReady += this.SkeletonsReady;
                _sensor.SkeletonStream.Enable(new TransformSmoothParameters()
                {
                    Smoothing = 0.5f,
                    Correction = 0.5f,
                    Prediction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f
                });

                _sensor.Start();
                Console.WriteLine(_sensor.SkeletonStream);
            }
            catch (Exception)
            {
                this.AppConflictOccurred();
                return;
            }
        }
        #endregion
    }
}
