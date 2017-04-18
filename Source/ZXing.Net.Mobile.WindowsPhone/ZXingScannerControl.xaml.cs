using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace ZXing.Mobile
{
    public partial class ZXingScannerControl : UserControl, IDisposable, IScannerView
    {
        public ZXingScannerControl() : base()
        {
            InitializeComponent();
        }

        public void StartScanning(Action<ZXing.Result> scanCallback, MobileBarcodeScanningOptions options = null)
        {
            ScanCallback = scanCallback;
            ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

            this.topText.Text = TopText;
            this.bottomText.Text = BottomText;

            if (UseCustomOverlay)
            {
                gridCustomOverlay.Children.Clear();
                if (CustomOverlay != null)
                    gridCustomOverlay.Children.Add(CustomOverlay);

                gridCustomOverlay.Visibility = Visibility.Visible;
                gridDefaultOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                gridCustomOverlay.Visibility = Visibility.Collapsed;
                gridDefaultOverlay.Visibility = Visibility.Visible;
            }

            MobileBarcodeScanner.Log("ZXingScannerControl.StartScanning");

            // Initialize a new instance of SimpleCameraReader with Auto-Focus mode on
            if (_reader == null)
            {
                MobileBarcodeScanner.Log("Creating SimpleCameraReader");

                //_reader = new SimpleCameraReader(options);
                _reader = new AudioVideoCaptureDeviceCameraReader(options);
                _reader.ScanInterval = ScanningOptions.DelayBetweenAnalyzingFrames;

                // AudioVideoCaptureDevice - move this to camera initialized otherwise throws
                // We need to set the VideoBrush we're going to display the preview feed on
                // IMPORTANT that it gets set before Camera initializes
                //_previewVideo.SetSource(_reader.Camera);

                // The reader throws an event when a result is available 
                _reader.DecodingCompleted += (o, r) => DisplayResult(r);

                // The reader throws an event when the camera is initialized and ready to use
                _reader.CameraInitialized += ReaderOnCameraInitialized;
            }
        }

        public Action<Result> ScanCallback { get; set; }
        public MobileBarcodeScanningOptions ScanningOptions { get; set; }
        public MobileBarcodeScannerBase Scanner { get; set; }
        public UIElement CustomOverlay { get; set; }
        public string TopText { get; set; }
        public string BottomText { get; set; }
        public bool UseCustomOverlay { get; set; }
        public bool ContinuousScanning { get; set; }

        public Result LastScanResult { get; set; }

        //SimpleCameraReader _reader;
        AudioVideoCaptureDeviceCameraReader _reader;

        public bool IsTorchOn
        {
            get { return _reader.FlashMode == FlashMode.On; }
        }

        public void Torch(bool on)
        {
            _reader.FlashMode = on ? FlashMode.On : FlashMode.Off;
        }

        public void ToggleTorch()
        {
            _reader.FlashMode = _reader.FlashMode == FlashMode.Off ? FlashMode.On : FlashMode.Off;
        }

        public bool HasTorch
        {
            get
            {
                return _reader.HasTorch;
            }
        }

        public void AutoFocus()
        {
            _reader.Focus();
        }

        public void AutoFocus (int x = -1, int y = -1)
        {
            _reader.Focus();
        }

        public bool IsAnalyzing
        {
            get {
                if (_reader != null)
                    return _reader.IsAnalyzing;

                return false;
            }
        }

        public void ResumeAnalysis ()
        {
            if (_reader != null && !_reader.IsAnalyzing)
                _reader.IsAnalyzing = false;
        }

        public void PauseAnalysis ()
        {
            if (_reader != null && _reader.IsAnalyzing)
                _reader.IsAnalyzing = false;
        }

        public void StopScanning()
        {
            if (UseCustomOverlay && CustomOverlay != null)
                gridCustomOverlay.Children.Remove(CustomOverlay);

            BlackoutVideoBrush();

            if (_reader != null)
            {
                _reader.Stop();
                _reader.CameraInitialized -= ReaderOnCameraInitialized;
		_reader.DecodingCompleted -= (o, r) => DisplayResult(r);
                _reader = null;
            }
        }

        private void BlackoutVideoBrush()
        {
        	_previewVideo.SetSource(new MediaElement());
        }
        
        public void Cancel()
        {
            LastScanResult = null;

			StopScanning ();

            if (ScanCallback != null)
                ScanCallback(null);
        }
        
        private void ReaderOnCameraInitialized(object sender, bool initialized)
        {
            // We dispatch (invoke) to avoid access exceptions
            Dispatcher.BeginInvoke(() =>
            {
                // This must be called here - until now, _reader.Camera is null
                _previewVideo.SetSource(_reader.Camera);

                if (_reader != null && _previewTransform != null)
                    _previewTransform.Rotation = _reader.CameraOrientation;
            });

            MobileBarcodeScanner.Log("ReaderOnCameraInitialized");

            if (_reader != null)
            {
                // We can set if Camera should flash or not when focused
                _reader.FlashMode = FlashMode.Off;

                // Starts the capturing process
                _reader.Start();
            }
        }
        
        private void DisplayResult(Result result)
        {
            if (!ContinuousScanning)
			    StopScanning ();

            if (ScanCallback != null)
                ScanCallback(result);
        }

        public void Dispose()
        {
            this.gridCustomOverlay.Children.Clear();

			StopScanning (); 
        }

        protected override void OnTap(System.Windows.Input.GestureEventArgs e)
        {
            base.OnTap(e);

            if (_reader != null) 
            {
                //var pos = e.GetPosition(this);
                _reader.Focus();
            }
        }

        private void buttonToggleFlash_Click(object sender, RoutedEventArgs e)
        {
            ToggleTorch();
        }
    }
}
