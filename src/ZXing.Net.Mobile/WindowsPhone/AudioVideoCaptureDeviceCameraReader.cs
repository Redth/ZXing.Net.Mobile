using Microsoft.Devices;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Phone.Media.Capture;

namespace ZXing.Mobile
{
    /// <summary>
    /// Replacement camera preview reader/processor that uses SL8 APIs and fixes issues with turning on the flash/torch.
    /// Written by Rodrigo 'r2d2rigo' Diaz
    /// http://www.r2d2rigo.es
    /// Based on SimpleCameraReader written by Henning M. Stephansen
    /// http://www.henning.ms
    /// </summary>
    public class AudioVideoCaptureDeviceCameraReader
    {
        public delegate void DecodingCompletedEventHandler(object sender, Result result);

        public event DecodingCompletedEventHandler DecodingCompleted;

        public delegate void CameraInitializedEventHandler(object sender, bool initialized);

        public event CameraInitializedEventHandler CameraInitialized;

        private DispatcherTimer _timer;
        private PhotoCameraLuminanceSource _luminance;
        private MultiFormatReader _reader;
        private AudioVideoCaptureDevice _photoCamera;

        private TimeSpan _scanInterval;
        private DateTime _lastAnalysis = DateTime.MinValue;

        bool doCancel = false;
        private bool _initialized;
        private bool _wasScanned = false;

        private VideoBrush _surface;

        private Dispatcher uiDispatcher;

        //private System.Threading.CancellationTokenSource cancelTokenSource = new System.Threading.CancellationTokenSource();

        /// <summary>
        /// Sets how often we should try to decode
        /// the camera feed for codes
        /// </summary>
        public double ScanInterval
        {
            get
            {
                return _scanInterval.TotalMilliseconds <= 0 ? _scanInterval.TotalMilliseconds : 150;
            }
            set
            {
                _scanInterval = TimeSpan.FromMilliseconds(value);
            }
        }

        /// <summary>
        /// Gets the number of degrees that the viewfinder brush needs to be rotated 
        /// clockwise to align with the camera sensor
        /// </summary>
        public double CameraOrientation
        {
            get
            {
                return _photoCamera != null ? (double)_photoCamera.SensorRotationInDegrees : 0;
            }
        }

        /// <summary>
        /// Returns the AudioVideoCaptureDevice instance 
        /// </summary>
        public AudioVideoCaptureDevice Camera
        {
            get
            {
                return _photoCamera;
            }
        }

        /// <summary>
        /// Get or set whether the camera should flash on auto-focus or not
        /// </summary>
        public FlashMode FlashMode
        {
            get
            {
                if (_initialized && _photoCamera != null)
                {
                    var supportedCameraModes = AudioVideoCaptureDevice.GetSupportedPropertyValues(_photoCamera.SensorLocation, KnownCameraAudioVideoProperties.VideoTorchMode);

                    if (supportedCameraModes.ToList().Contains((UInt32)VideoTorchMode.On))
                    {
                        var torchStatus = (VideoTorchMode)_photoCamera.GetProperty(KnownCameraAudioVideoProperties.VideoTorchMode);

                        switch (torchStatus)
                        {
                            default:
                            case VideoTorchMode.Off:
                                return FlashMode.Off;
                            case VideoTorchMode.On:
                                return FlashMode.On;
                            case VideoTorchMode.Auto:
                                return FlashMode.Auto;
                        }
                    }
                }

                return FlashMode.Off;
            }
            set
            {
                if (_photoCamera != null)
                {
                    var supportedCameraModes = AudioVideoCaptureDevice.GetSupportedPropertyValues(_photoCamera.SensorLocation, KnownCameraAudioVideoProperties.VideoTorchMode);

                    if (supportedCameraModes.ToList().Contains((UInt32)VideoTorchMode.On))
                    {
                        VideoTorchMode torchStatus = VideoTorchMode.Off;

                        switch (value)
                        {
                            default:
                            case FlashMode.Off:
                                torchStatus = VideoTorchMode.Off;
                                break;
                            case FlashMode.On:
                                torchStatus = VideoTorchMode.On;
                                break;
                            case FlashMode.Auto:
                                torchStatus = VideoTorchMode.Auto;
                                break;
                        }

                        _photoCamera.SetProperty(KnownCameraAudioVideoProperties.VideoTorchMode, torchStatus);
                    }
                }
            }
        }

        public MobileBarcodeScanningOptions Options { get; set; }
        //public MobileBarcodeScannerBase Scanner { get; set; }

        /// <summary>
        /// Initializes the SimpleCameraReader
        /// </summary>
        /// <param name="scanOnAutoFocus">Sets whether the camera should scan on completed autofocus or on a timely fashion</param>
        public AudioVideoCaptureDeviceCameraReader(MobileBarcodeScanningOptions options)
        {
            this.Options = options ?? MobileBarcodeScanningOptions.Default;
            // this.Scanner = scanner;

            Initialize();
        }

        private void Initialize()
        {
            //ScanOnAutoFocus = true; // scanOnAutoFocus;

            // Gets the Dispatcher for the current application so we can invoke the UI-Thread to get
            // preview-image and fire our timer events
            uiDispatcher = Application.Current.RootVisual.Dispatcher;

            InitializeCamera();


            _reader = this.Options.BuildMultiFormatReader();
        }

        private async Task InitializeCamera()
        {
            if (Options.UseFrontCameraIfAvailable.HasValue && Options.UseFrontCameraIfAvailable.Value)
            {
                try
                {
                    var frontCameraCaptureResolutions = AudioVideoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Front);
                    _photoCamera = await AudioVideoCaptureDevice.OpenForVideoOnlyAsync(CameraSensorLocation.Front, frontCameraCaptureResolutions.OrderBy(r => r.Width * r.Height).First());
                }
                catch (Exception ex)
                {
                    MobileBarcodeScanner.Log("Failed to create front facing camera: {0}", ex);
                }
            }

            MobileBarcodeScanner.Log("InitializeCamera");

            if (_photoCamera == null)
            {
                var backCameraCaptureResolutions = AudioVideoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back);
                _photoCamera = await AudioVideoCaptureDevice.OpenForVideoOnlyAsync(CameraSensorLocation.Back, backCameraCaptureResolutions.OrderBy(r => r.Width * r.Height).First());
            }

            OnPhotoCameraInitialized(this, new CameraOperationCompletedEventArgs(true, null));

            MobileBarcodeScanner.Log("Wired up Initizialied");
        }

        /// <summary>
        /// Starts the camera and capture process
        /// </summary>
        public void Start()
        {
            if (_photoCamera == null)
                InitializeCamera();

            // At this point if application exits etc. without proper stopping of camera
            // it will throw an Exception. 
            try
            {
                // Invokes these method calls on the UI-thread
                uiDispatcher.BeginInvoke(() =>
                {
                    _timer = new DispatcherTimer();
                    _timer.Interval = _scanInterval;
                    _timer.Tick += (o, arg) => ScanPreviewBuffer();

                    CameraButtons.ShutterKeyHalfPressed += CameraButtons_ShutterKeyHalfPressed;

                    _timer.Start();
                });

            }
            catch (Exception)
            {
                // Do nothing
            }

            Focus();
        }

        private void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            Focus();
        }

        public async Task Focus()
        {
            try
            {
                await _photoCamera.FocusAsync();
            }
            catch { }
        }

        public void Focus(Point point)
        {
            try
            {
                // TODO: point focusing
                //if (_photoCamera.IsFocusAtPointSupported)
                //    _photoCamera.FocusAtPoint(point.X, point.Y);
                //else
                Focus();
            }
            catch { }
        }

        /// <summary>
        /// Stops the camera and capture process
        /// </summary>
        public void Stop()
        {
            doCancel = true;

            if (_timer != null && _timer.IsEnabled)
                _timer.Stop();

            if (_photoCamera != null)
            {
                CameraButtons.ShutterKeyHalfPressed -= CameraButtons_ShutterKeyHalfPressed;

                _photoCamera.Dispose();
                _photoCamera = null;
            }
        }

        private void OnPhotoCameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            MobileBarcodeScanner.Log("Initialized Camera");

            if (_photoCamera == null)
                return;

            MobileBarcodeScanner.Log("Creating Luminance Source");

            var width = Convert.ToInt32(_photoCamera.PreviewResolution.Width);
            var height = Convert.ToInt32(_photoCamera.PreviewResolution.Height);

            _luminance = new PhotoCameraLuminanceSource(width, height);

            var supportedCameraModes = AudioVideoCaptureDevice.GetSupportedPropertyValues(_photoCamera.SensorLocation, KnownCameraAudioVideoProperties.VideoTorchMode);
            if (supportedCameraModes.ToList().Contains((UInt32)VideoTorchMode.On))
            {
                _photoCamera.SetProperty(KnownCameraAudioVideoProperties.VideoTorchMode, VideoTorchMode.Off);
            }

            _initialized = true;

            MobileBarcodeScanner.Log("Luminance Source Created");

            OnCameraInitialized(_initialized);
        }

        private void ScanPreviewBuffer()
        {
            if (_photoCamera == null) return;
            if (!_initialized) return;

            // Don't scan too frequently
            // Check the minimum time between frames
            // as well as the min time between continuous scans
            var msSinceLastPreview = (DateTime.UtcNow - _lastAnalysis).TotalMilliseconds;
            if ((DateTime.UtcNow - _lastAnalysis).TotalMilliseconds < Options.DelayBetweenAnalyzingFrames
                || (_wasScanned && msSinceLastPreview < Options.DelayBetweenContinuousScans))
                return;

            _wasScanned = false;
            _lastAnalysis = DateTime.UtcNow;

            try
            {
                _photoCamera.GetPreviewBufferY(_luminance.PreviewBufferY);
                var binarizer = new ZXing.Common.HybridBinarizer(_luminance);

                var binBitmap = new BinaryBitmap(binarizer);

                var result = _reader.decode(binBitmap);

                if (result != null)
                {
                    _wasScanned = true;
                    OnDecodingCompleted(result);
                }
            }
            catch (Exception)
            {
                // If decoding fails it will throw a ReaderException
                // and we're not interested in doing anything with it
            }
        }

        /// <summary>
        /// When a decode was successful we get a result back
        /// </summary>
        /// <param name="result">Result of the decoding</param>
        protected virtual void OnDecodingCompleted(Result result)
        {
            if (DecodingCompleted != null)
                DecodingCompleted(this, result);
        }

        /// <summary>
        /// When camera is finished initializing and ready to go. Put Start after this event has fired
        /// </summary>
        /// <param name="initialized">A boolean value indicating whether camera was successfully initialized</param>
        protected virtual void OnCameraInitialized(bool initialized)
        {
            if (CameraInitialized != null)
                CameraInitialized(this, initialized);
        }
    }
}
