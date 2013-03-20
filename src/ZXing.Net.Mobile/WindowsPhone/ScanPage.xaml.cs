using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using ZXing;

namespace ZXing.Mobile
{
	public partial class ScanPage : PhoneApplicationPage
	{
        public static MobileBarcodeScanningOptions ScanningOptions { get; set; }
        public static MobileBarcodeScannerBase Scanner { get; set; }
        public static System.Windows.UIElement CustomOverlay { get; set; }
        public static string TopText { get; set; }
        public static string BottomText { get; set; }
        public static bool UseCustomOverlay { get; set; }

        public static Result LastScanResult { get; set; }

		SimpleCameraReader _reader;

        public static Action<Result> FinishedAction { get; set; }

        public static event Action<bool> OnRequestTorch;
        public static event Action OnRequestToggleTorch;
        public static event Action OnRequestAutoFocus;
        public static event Action OnRequestCancel;
        public static event Func<bool> OnRequestIsTorchOn;
        
        public static bool RequestIsTorchOn()
        {
            var evt = OnRequestIsTorchOn;
            if (evt != null)
                return evt();
            else
                return false;
        }

        public static void RequestTorch(bool on)
        {
            var evt = OnRequestTorch;
            if (evt != null)
                evt(on);
        }

        public static void RequestToggleTorch()
        {
            var evt = OnRequestToggleTorch;
            if (evt != null)
                evt();
        }

        public static void RequestAutoFocus()
        {
            var evt = OnRequestAutoFocus;
            if (evt != null)
                evt();
        }

        public static void RequestCancel()
        {
            var evt = OnRequestCancel;
            if (evt != null)
                evt();
        }

		public ScanPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.topText.Text = TopText;
            this.bottomText.Text = BottomText;

            if (UseCustomOverlay && CustomOverlay != null)
            {
               this.gridCustomOverlay.Children.Add(CustomOverlay);
    
                this.gridCustomOverlay.Visibility = System.Windows.Visibility.Visible;
                this.gridDefaultOverlay.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                //this.gridCustomOverlay.Children.Clear();
                this.gridCustomOverlay.Visibility = System.Windows.Visibility.Collapsed;
                this.gridDefaultOverlay.Visibility = System.Windows.Visibility.Visible;
            }

            // Initialize a new instance of SimpleCameraReader with Auto-Focus mode on
            _reader = new SimpleCameraReader(Scanner, ScanningOptions);
			_reader.ScanInterval = ScanningOptions.DelayBetweenAnalyzingFrames;

            OnRequestAutoFocus += () =>
            {
                _reader.Camera.Focus();
            };

            OnRequestTorch += (on) =>
            {
                if (on)
                    _reader.Camera.FlashMode = FlashMode.On;
                else
                    _reader.Camera.FlashMode = FlashMode.Auto;
            };

            OnRequestToggleTorch += () => {
                if (_reader.Camera.FlashMode == FlashMode.On)
                    _reader.Camera.FlashMode = FlashMode.Auto;
                else
                    _reader.Camera.FlashMode = FlashMode.On;
            };

            OnRequestCancel += () => {
                LastScanResult = null;

                _reader.Stop();

                Finish(); 
            };

            OnRequestIsTorchOn += () => {
                return _reader.Camera.FlashMode == FlashMode.On;
            };
            // We need to set the VideoBrush we're going to display the preview feed on
            // IMPORTANT that it gets set before Camera initializes
            //a   _previewVideo.SetSource((CaptureSource)_reader.Camera);

	        //		_reader.ScanOnAutoFocus = false;
			
			_previewVideo.SetSource(_reader.Camera);

			 
            // The reader throws an event when a result is available 
            _reader.DecodingCompleted += (o, r) => DisplayResult(r);

            // The reader throws an event when the camera is initialized and ready to use
            _reader.CameraInitialized += ReaderOnCameraInitialized;

            base.OnNavigatedTo(e);


        }

        private void ReaderOnCameraInitialized(object sender, bool initialized)
        {
            // We dispatch (invoke) to avoid access exceptions
            Dispatcher.BeginInvoke(() =>
            {
                _previewTransform.Rotation = _reader.CameraOrientation;
            });

            // We can set if Camera should flash or not when focused
			_reader.FlashMode = Microsoft.Devices.FlashMode.Off;

            // Starts the capturing process
            _reader.Start();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            this.gridCustomOverlay.Children.Clear();

            _reader.Stop();

            base.OnNavigatingFrom(e);
        }

		bool successScan = false;

        private void DisplayResult(Result result)
        {
			_reader.Stop();

			successScan = true;

            if (result != null)
                LastScanResult = result;
            else
                LastScanResult = null;


            Finish();
        }

        void Finish()
        {
            var evt = FinishedAction;
            if (evt != null)
                evt(LastScanResult);
        }
	}
}