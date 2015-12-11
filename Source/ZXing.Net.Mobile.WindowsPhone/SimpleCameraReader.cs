using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Devices;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ZXing;

namespace ZXing.Mobile
{
	/// <summary>
	/// A simple and easy to use wrapper for ZXing 2.0 for use with Windows Phone 7
	/// Written by Henning M. Stephansen
	/// http://www.henning.ms
	/// </summary>
	public class SimpleCameraReader
	{
		public delegate void DecodingCompletedEventHandler(object sender, Result result);

		public event DecodingCompletedEventHandler DecodingCompleted;

		public delegate void CameraInitializedEventHandler(object sender, bool initialized);

		public event CameraInitializedEventHandler CameraInitialized;
       
		private DispatcherTimer _timer;
		private PhotoCameraLuminanceSource _luminance;
		private MultiFormatReader _reader;
		private PhotoCamera _photoCamera;

		private TimeSpan _scanInterval;
        private DateTime _lastAnalysis = DateTime.MinValue;

        bool doCancel = false;
		private bool _initialized;
        private bool _wasScanned = false;
        public bool IsAnalyzing { get; set; }

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
				return _photoCamera != null ? _photoCamera.Orientation : 0;
			}
		}

		/// <summary>
		/// Returns the PhotoCamera instance 
		/// </summary>
		public PhotoCamera Camera
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
				if (_initialized && _photoCamera != null && _photoCamera.IsFlashModeSupported(FlashMode.On))
					return _photoCamera.FlashMode != FlashMode.Off ? _photoCamera.FlashMode : FlashMode.Off;

				return FlashMode.Off;
			}
			set
			{
				if (_photoCamera != null && _photoCamera.IsFlashModeSupported(value))
					_photoCamera.FlashMode = value;
			}
		}

        public MobileBarcodeScanningOptions Options { get; set; }
        //public MobileBarcodeScannerBase Scanner { get; set; }

		/// <summary>
		/// Initializes the SimpleCameraReader
		/// </summary>
		/// <param name="scanOnAutoFocus">Sets whether the camera should scan on completed autofocus or on a timely fashion</param>
		public SimpleCameraReader(MobileBarcodeScanningOptions options)
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

		private void InitializeCamera()
		{
            if (Options.UseFrontCameraIfAvailable.HasValue && Options.UseFrontCameraIfAvailable.Value)
            {
                try { _photoCamera = new PhotoCamera(CameraType.FrontFacing); }
                catch (Exception ex) {
                    MobileBarcodeScanner.Log ("Failed to create front facing camera: {0}", ex);
                }
            }

            MobileBarcodeScanner.Log("InitializeCamera");

            if (_photoCamera == null)
    			_photoCamera = new PhotoCamera();
			_photoCamera.Initialized += OnPhotoCameraInitialized;

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

        public void Focus()
        {
            try
            {
                if (_photoCamera.IsFocusSupported)
                    _photoCamera.Focus();
            }
            catch { }
        }

        public void Focus(Point point)
        {
            try
            {
                if (_photoCamera.IsFocusAtPointSupported)
                    _photoCamera.FocusAtPoint(point.X, point.Y);
                else
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
            IsAnalyzing = false;

			if (_timer != null && _timer.IsEnabled)
				_timer.Stop();

			if (_photoCamera != null)
			{
                CameraButtons.ShutterKeyHalfPressed -= CameraButtons_ShutterKeyHalfPressed;

                _photoCamera.Initialized -= OnPhotoCameraInitialized;
				_photoCamera.Dispose();
				_photoCamera = null;
			}

		}

		private void OnPhotoCameraInitialized(object sender, CameraOperationCompletedEventArgs e)
		{
            MobileBarcodeScanner.Log("Initialized Camera");

            if (_photoCamera == null)
                return;
            IsAnalyzing = true;

            MobileBarcodeScanner.Log("Creating Luminance Source");

			var width = Convert.ToInt32(_photoCamera.PreviewResolution.Width);
			var height = Convert.ToInt32(_photoCamera.PreviewResolution.Height);

			_luminance = new PhotoCameraLuminanceSource(width, height);

            if (_photoCamera.IsFlashModeSupported(FlashMode.On))
    			_photoCamera.FlashMode = FlashMode.Off;

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

            if (!IsAnalyzing)
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
