using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using Android.Graphics;
using Android.Content;
using Android.Util;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraAnalyzer
    {
        /// <summary>
        ///START - Scanning Improvement, VK 10/2018
        /// </summary>
        private const int MIN_FRAME_WIDTH = 240;
        private const int MIN_FRAME_HEIGHT = 240;
        private const int MAX_FRAME_WIDTH = 640; // = 5/8 * 1920
        private const int MAX_FRAME_HEIGHT = 480; // = 5/8 * 1080
        /// <summary>
        /// END - Scanning Improvement, VK 10/2018
        /// </summary>
        /// 

        private readonly CameraController _cameraController;
        private readonly CameraEventsListener _cameraEventListener;
        private int _screenHeight = -1;
        private int _screenWidth = -1;
        private Task _processingTask;
        private DateTime _lastPreviewAnalysis = DateTime.UtcNow;
        private bool _wasScanned;
        IScannerSessionHost _scannerHost;
        private Rect framingRectInPreview;
        private Rect framingRect;
        private IWindowManager manager;

        public CameraAnalyzer(SurfaceView surfaceView, IScannerSessionHost scannerHost)
        {
            _scannerHost = scannerHost;
            _cameraEventListener = new CameraEventsListener();
            _cameraController = new CameraController(surfaceView, _cameraEventListener, scannerHost);
            Torch = new Torch(_cameraController, surfaceView.Context);
            try
            {
                manager = (surfaceView.Context as ZxingActivity)?.WindowManager;
            }
            catch(Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Error occured while getting window manager : " + ex.ToString());
            }
        }

        public event EventHandler<Result> BarcodeFound;

        public Torch Torch { get; }

        public bool IsAnalyzing { get; private set; }

        public void PauseAnalysis()
        {
            IsAnalyzing = false;
        }

        public void ResumeAnalysis()
        {
            IsAnalyzing = true;
        }

        public void ShutdownCamera()
        {
            IsAnalyzing = false;
            _cameraEventListener.OnPreviewFrameReady -= HandleOnPreviewFrameReady;
            _cameraController.ShutdownCamera();
        }

        public void SetupCamera()
        {
            _cameraEventListener.OnPreviewFrameReady += HandleOnPreviewFrameReady;
            _cameraController.SetupCamera();
        }

        public void AutoFocus()
        {
            _cameraController.AutoFocus();
        }

        /// <summary>
        ///Scanning Improvement, VK 10/2018
        /// </summary>
        //public void LowLightMode(bool on)
        //{
        //    _cameraController.LowLightMode(on);
        //}

        public void AutoFocus(int x, int y)
        {
            _cameraController.AutoFocus(x, y);
        }

        public void RefreshCamera()
        {
            _cameraController.RefreshCamera();
        }

        private bool Valid_ScreenResolution
        {
            get
            {
                return _screenHeight > 0 && _screenWidth > 0;
            }
        }

        private bool CanAnalyzeFrame
        {
            get
            {
				if (!IsAnalyzing)
					return false;
				
                //Check and see if we're still processing a previous frame
                // todo: check if we can run as many as possible or mby run two analyzers at once (Vision + ZXing)
                if (_processingTask != null && !_processingTask.IsCompleted)
                    return false;
                
                var elapsedTimeMs = (DateTime.UtcNow - _lastPreviewAnalysis).TotalMilliseconds;
				if (elapsedTimeMs < _scannerHost.ScanningOptions.DelayBetweenAnalyzingFrames)
					return false;
				
				// Delay a minimum between scans
				if (_wasScanned && elapsedTimeMs < _scannerHost.ScanningOptions.DelayBetweenContinuousScans)
					return false;
				
				return true;
            }
        }

        private void HandleOnPreviewFrameReady(object sender, FastJavaByteArray fastArray)
        {
            if (!CanAnalyzeFrame)
                return;

            _wasScanned = false;
            _lastPreviewAnalysis = DateTime.UtcNow;

			_processingTask = Task.Run(() =>
			{
				try
				{
                    Log.Debug(MobileBarcodeScanner.TAG, "Preview Analyzing.");
                    DecodeFrame(fastArray);
				} catch (Exception ex) {
					Console.WriteLine(ex);
				}
			}).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Log.Debug(MobileBarcodeScanner.TAG, "DecodeFrame exception occurs");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void DecodeFrame(FastJavaByteArray fastArray)
        {
            var cameraParameters = _cameraController.Camera.GetParameters();
            var width = cameraParameters.PreviewSize.Width;
            var height = cameraParameters.PreviewSize.Height;

            var barcodeReader = _scannerHost.ScanningOptions.BuildBarcodeReader();

            var rotate = false;
            var newWidth = width;
            var newHeight = height;

            // use last value for performance gain
            var cDegrees = _cameraController.LastCameraDisplayOrientationDegree;

            if (cDegrees == 90 || cDegrees == 270)
            {
                rotate = true;
                newWidth = height;
                newHeight = width;
            }

            ZXing.Result result = null;
            var start = PerformanceCounter.Start();

            /// <summary>
            ///START - Scanning Improvement, VK 10/2018
            /// </summary>
            var frame_width = width * 3 / 5;
            var frame_height = height * 3 / 5;
            var frame_left = width * 1 / 5;
            var frame_top = height * 1 / 5;
           
            LuminanceSource fast = new FastJavaByteArrayYUVLuminanceSource(fastArray, width, height,
                                                                            //framingRectPreview?.Width() ?? width,
                                                                           // framingRectPreview?.Height() ?? height,
                                                                            frame_left,
                                                                            frame_top,
                                                                            frame_width,
                                                                            frame_height); // _area.Left, _area.Top, _area.Width, _area.Height);

            /// <summary>
            ///END - Scanning Improvement, VK 10/2018
            /// </summary>
            if (rotate)
                fast = fast.rotateCounterClockwise();

            result = barcodeReader.Decode(fast);

            fastArray.Dispose();
            fastArray = null;

            PerformanceCounter.Stop(start,
                $"width: {width}, height: {height}, frame_top :{frame_top}, frame_left: {frame_left}, frame_width: {frame_width}, frame_height: {frame_height}, degrees: {cDegrees}, rotate: {rotate}; " + "Decode Time: {0} ms");

            if (result != null)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Barcode Found");

                _wasScanned = true;
                BarcodeFound?.Invoke(this, result);
                return;
            }
        }


        /// <summary>
        ///Scanning Improvement, VK 10/2018
        /// </summary>
        private Rect GetFramingRectInPreview()
        {
            if (framingRectInPreview == null)
            {
                //if (!Valid_ScreenResolution)
                //    GetScreenResolution();
                var cameraParameters = _cameraController?.Camera?.GetParameters();
                var width = cameraParameters.PreviewSize.Width;
                var height = cameraParameters.PreviewSize.Height;
                if (cameraParameters == null)//|| !Valid_ScreenResolution)
                {
                    // Called early, before init even finished
                    return null;
                }

                var framingRect = GetFramingRect(width, height);
                if (framingRect == null)
                {
                    return null;
                }

                var rect = new Rect(framingRect);
                //var cameraParameters = _cameraController?.Camera?.GetParameters();
                //var width = cameraParameters.PreviewSize.Width;
                //var height = cameraParameters.PreviewSize.Height;
            

                //rect.Left = rect.Left * width / _screenWidth;
                //rect.Right = rect.Right * width / _screenHeight;
                //rect.Top = rect.Top * height / _screenWidth;
                //rect.Bottom = rect.Bottom * height / _screenHeight;
                framingRectInPreview = rect;
                Log.Debug(MobileBarcodeScanner.TAG, $"preview resolution: w={width}; h={height}; _screenWidth ={_screenWidth}; _screenHeight={_screenHeight}; framingRect={framingRect?.ToString()}");
            }

            Log.Debug(MobileBarcodeScanner.TAG, $"Calculated preview framing rect: {framingRectInPreview?.FlattenToString()}");
            return framingRectInPreview;
        }

        /// <summary>
        ///Scanning Improvement, VK 10/2018
        /// </summary>
        public Rect GetFramingRect(int _width, int _height)
        {
            if (framingRect == null)
            {
                if (_cameraController == null)
                {
                    return null;
                }

                if (!(_width > 0 && _height > 0))//Valid_ScreenResolution)
                {
                    // Called early, before init even finished
                    return null;
                }

                int width = findDesiredDimensionInRange(_width, MIN_FRAME_WIDTH, MAX_FRAME_WIDTH);
                int height = findDesiredDimensionInRange(_height, MIN_FRAME_HEIGHT, MAX_FRAME_HEIGHT);

                int leftOffset = (_width - width) / 2;
                int topOffset = (_height - height) / 2;
                framingRect = new Rect(leftOffset, topOffset, width, height);
                Log.Debug(MobileBarcodeScanner.TAG, $"Calculated framing rect: {framingRect?.FlattenToString()}; screenWidth: {_screenWidth}; screenHeight: {_screenHeight}");
            }

            return framingRect;
        }

        /// <summary>
        ///Scanning Improvement, VK 10/2018
        /// </summary>
        private void GetScreenResolution()
        {
            var screenResolution = new DisplayMetrics();
            try
            {
                if(manager == null)
                {
                    Log.Debug(MobileBarcodeScanner.TAG, $"Window manager is null.");
                }

                Display display = manager?.DefaultDisplay;
                if (display == null)
                {
                    Log.Debug(MobileBarcodeScanner.TAG, $"Default display is null.");
                }
                else
                {

                    display?.GetMetrics(screenResolution);
                    _screenWidth = screenResolution.WidthPixels;
                    _screenHeight = screenResolution.HeightPixels;
                }
                Log.Debug(MobileBarcodeScanner.TAG, $"Screen Display Rect-  Width = {_screenWidth}; Height = {_screenHeight} ");
            }
            catch (Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Error occured while getting screen resolution : " + ex.ToString());
            }
        }

        /// <summary>
        ///Scanning Improvement, VK 10/2018
        /// </summary>
        private int findDesiredDimensionInRange(int resolution, int hardMin, int hardMax)
        {
            int dim = 5 * resolution / 8; // Target 5/8 of each dimension
            if (dim < hardMin)
            {
                return hardMin;
            }
            if (dim > hardMax)
            {
                return hardMax;
            }
            return dim;
        }
    }
}
