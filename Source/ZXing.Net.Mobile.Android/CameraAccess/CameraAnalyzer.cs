using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Views;
using ZXing.Mobile.Detectors;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraAnalyzer
    {
        private readonly CameraController _cameraController;
        private readonly Context _context;
        private readonly MobileBarcodeScanningOptions _scanningOptions;
        private readonly CameraEventsListener _cameraEventListener;
        private bool _wasScanned;
        private Task _processingTask;
        private IDetector _barcodeDetector;
        private DateTime _lastPreviewAnalysis = DateTime.UtcNow;

        public CameraAnalyzer(SurfaceView surfaceView, MobileBarcodeScanningOptions scanningOptions)
        {
            _context = surfaceView.Context;
            _scanningOptions = scanningOptions;
            _cameraEventListener = new CameraEventsListener();
            _cameraController = new CameraController(surfaceView, _cameraEventListener, scanningOptions);
            Torch = new Torch(_cameraController, _context);
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

        public void AutoFocus(int x, int y)
        {
            _cameraController.AutoFocus(x, y);
        }

        public void RefreshCamera()
        {
            _cameraController.RefreshCamera();
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
                {
                    return false;
                }

                var elapsedTimeMs = (DateTime.UtcNow - _lastPreviewAnalysis).TotalMilliseconds;
                if (elapsedTimeMs < _scanningOptions.DelayBetweenAnalyzingFrames)
                    return false;

                // Delay a minimum between scans
                if (_wasScanned && elapsedTimeMs < _scanningOptions.DelayBetweenContinuousScans)
                    return false;

                return true;
            }
        }

        private void HandleOnPreviewFrameReady(object sender, byte[] bytes)
        {
            if (!CanAnalyzeFrame)
                return;

            _wasScanned = false;
            _lastPreviewAnalysis = DateTime.UtcNow;

            _processingTask = Task.Run(() => DecodeFrame(bytes)).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "DecodeFrame exception occurs");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void DecodeFrame(byte[] bytes)
        {
            var cameraParameters = _cameraController.Camera.GetParameters();
            var width = cameraParameters.PreviewSize.Width;
            var height = cameraParameters.PreviewSize.Height;

            InitBarcodeReaderIfNeeded();

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

            var start = PerformanceCounter.Start();

            if (rotate)
                bytes = RotateCounterClockwise(bytes, width, height);

            var result = _barcodeDetector.Decode(bytes, newWidth, newHeight);

            PerformanceCounter.Stop(start,
                "Decode Time: {0} ms (width: " + width + ", height: " + height + ", degrees: " + cDegrees + ", rotate: " +
                rotate + ")");

            if ((result == null) || string.IsNullOrEmpty(result.Text))
                return;

            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Barcode Found: " + result.Text);

            _wasScanned = true;
            BarcodeFound?.Invoke(this, result);
        }

        private void InitBarcodeReaderIfNeeded()
        {
            if (_barcodeDetector != null)
                return;

            var isNativeUsed = false;
            if (_scanningOptions.UseNativeScanning)
            {
                _barcodeDetector = CreateGoogleVisionDetector();
                isNativeUsed = true;
            }

            if (_barcodeDetector == null)
            {
                _barcodeDetector = new ZXingDetector();
                isNativeUsed = false;
            }

            var isDetectorAvailable = _barcodeDetector.Init(_scanningOptions);
            if (!isDetectorAvailable && isNativeUsed)
            {
                _barcodeDetector = new ZXingDetector();
                _barcodeDetector.Init(_scanningOptions);
            }
        }

        private IDetector CreateGoogleVisionDetector()
        {
            try
            {
                var targetAsm =
                    AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(x => x.GetName().Name == "ZXing.Net.Mobile.Android.Vision");
                if (targetAsm != null)
                {
                    var type = targetAsm.GetType("ZXing.Net.Mobile.Android.Vision.GoogleVisionDetector");
                    if (type != null)
                    {
                        return Activator.CreateInstance(type, _context) as IDetector;
                    }
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(MobileBarcodeScanner.TAG, ex.ToString());
            }

            return null;
        }

        private static byte[] RotateCounterClockwise(byte[] data, int width, int height)
        {
            var rotatedData = new byte[data.Length];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    rotatedData[x*height + height - y - 1] = data[x + y*width];
            return rotatedData;
        }
    }
}