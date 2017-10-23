using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApxLabs.FastAndroidCamera;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraAnalyzer
    {
        private readonly CameraController _cameraController;
        private readonly MobileBarcodeScanningOptions _scanningOptions;
        private DateTime _lastPreviewAnalysis = DateTime.UtcNow;
        private bool _wasScanned;
        private BarcodeReaderGeneric<FastJavaByteArrayYUVLuminanceSource> _barcodeReader;

        public CameraAnalyzer(CameraController cameraController, MobileBarcodeScanningOptions scanningOptions)
        {
            _scanningOptions = scanningOptions;
            _cameraController = cameraController;

            Torch = new Torch(_cameraController);
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
            _cameraController.OnPreviewFrameReady -= HandleOnPreviewFrameReady;
            _cameraController.ShutdownCamera();
        }

        public void SetupCamera()
        {
            _cameraController.OnPreviewFrameReady += HandleOnPreviewFrameReady;
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
				
                var elapsedTimeMs = (DateTime.UtcNow - _lastPreviewAnalysis).TotalMilliseconds;
				if (elapsedTimeMs < _scanningOptions.DelayBetweenAnalyzingFrames)
					return false;
				
				// Delay a minimum between scans
				if (_wasScanned && elapsedTimeMs < _scanningOptions.DelayBetweenContinuousScans)
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

            try 
            {
                DecodeFrame(fastArray);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"DecodeFrame exception occured: {ex.Message}");
            }
        }

		private byte[] buffer;
        private void DecodeFrame(FastJavaByteArray fastArray)
        {
            var cameraParameters = _cameraController.Camera.GetParameters();
            var width = cameraParameters.PreviewSize.Width;
            var height = cameraParameters.PreviewSize.Height;

            InitBarcodeReaderIfNeeded();

            // use last value for performance gain
            var cDegrees = _cameraController.LastCameraDisplayOrientationDegree;
			var rotate = (cDegrees == 90 || cDegrees == 270);

            Result result = null;
            var start = PerformanceCounter.Start();

            if (rotate) 
            {
                fastArray.Transpose(ref buffer, width, height);
                var tmp = width;
                width = height;
                height = tmp;
            }
			
            var luminanceSource = new FastJavaByteArrayYUVLuminanceSource(fastArray, width, height, 0, 0, width, height); // _area.Left, _area.Top, _area.Width, _area.Height);
            
            result = _barcodeReader.Decode(luminanceSource);

            PerformanceCounter.Stop(start, "Decode Time: {0} ms (width: " + width + ", height: " + height + ", degrees: " + cDegrees + ", rotate: " + rotate + ")");

            if (result != null)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Barcode Found: " + result.Text);

                _wasScanned = true;
                BarcodeFound?.Invoke(this, result);
            }
            else
                AutoFocus();
        }

        private void InitBarcodeReaderIfNeeded()
        {
            if (_barcodeReader != null)
                return;

            _barcodeReader = new BarcodeReaderGeneric<FastJavaByteArrayYUVLuminanceSource>();

            if (_scanningOptions.TryHarder.HasValue)
                _barcodeReader.Options.TryHarder = _scanningOptions.TryHarder.Value;
            if (_scanningOptions.PureBarcode.HasValue)
                _barcodeReader.Options.PureBarcode = _scanningOptions.PureBarcode.Value;
            if (!string.IsNullOrEmpty(_scanningOptions.CharacterSet))
                _barcodeReader.Options.CharacterSet = _scanningOptions.CharacterSet;
            if (_scanningOptions.TryInverted.HasValue)
                _barcodeReader.TryInverted = _scanningOptions.TryInverted.Value;
            if (_scanningOptions.UseCode39ExtendedMode.HasValue)
                _barcodeReader.Options.UseCode39ExtendedMode = _scanningOptions.UseCode39ExtendedMode.Value;
            if (_scanningOptions.AssumeGS1.HasValue)
                _barcodeReader.Options.AssumeGS1 = _scanningOptions.AssumeGS1.Value;

            if (_scanningOptions.PossibleFormats != null && _scanningOptions.PossibleFormats.Count > 0)
            {
                _barcodeReader.Options.PossibleFormats = new List<BarcodeFormat>();

                foreach (var pf in _scanningOptions.PossibleFormats)
                    _barcodeReader.Options.PossibleFormats.Add(pf);
            }
        }
    }
}