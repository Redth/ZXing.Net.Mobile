using System;
using System.Linq;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace ZXing.Mobile
{
    class ZXingScannerCammera : Camera
    {
        private Action<Result> resultHandler;
        private bool isDisposed;
        private bool torchFlag;
        private CameraFlashMode torchMode;
        public MobileBarcodeScanningOptions scanningOptions;
        public bool IsTorchOn
        {
            get
            {
                return Settings.FlashMode == CameraFlashMode.On;
            }
            set
            {
                Settings.FlashMode = value ? CameraFlashMode.On : CameraFlashMode.Off;
            }
        }
        public bool HasTorch
        {
            get
            {
                return Capabilities.SupportedFlashModes.ToList().Count > 0;
            }
        }
        public ZXingScannerCammera(CameraDevice device, MediaView mediaView) : base(device)
        {
            Display = new Display(mediaView);
            Settings.ImageQuality = 100;
            Settings.PreviewPixelFormat = Capabilities.SupportedPreviewPixelFormats.FirstOrDefault();
            Settings.PreviewResolution = Settings.RecommendedPreviewResolution;
            Settings.CapturePixelFormat = CameraPixelFormat.Nv12;
            Settings.CaptureResolution = Capabilities.SupportedCaptureResolutions.FirstOrDefault();
            DisplaySettings.Rotation = Rotation.Rotate270;

            FocusStateChanged += FocusStateChangedHandler;
            Capturing += CapturingHandler;
            CaptureCompleted += CaptureCompleteHandler;
            StateChanged += StateChangeHandler;

            isDisposed = false;
            if (State != CameraState.Preview) StartPreview();
        }

        private void StateChangeHandler(object sender, CameraStateChangedEventArgs e)
        {
            if(!isDisposed && State == CameraState.Preview)
            {
                if (torchFlag)
                    Settings.FlashMode = torchMode;
                torchFlag = false;
            }
        }

        private async void CaptureCompleteHandler(object sender, EventArgs e)
        {
            if (!isDisposed) {
                if(scanningOptions?.DelayBetweenContinuousScans > 0)
                {
                    await Task.Delay(scanningOptions.DelayBetweenContinuousScans);
                }
                StartPreview();
            }
        }

        private async void CapturingHandler(object sender, CameraCapturingEventArgs e)
        {
            Result result = await TizenBarcodeAnalyzer.AnalyzeBarcodeAsync(e.MainImage);
            if (result != null)
                resultHandler?.Invoke(result);
        }

        private void FocusStateChangedHandler(object sender, CameraFocusStateChangedEventArgs e)
        {
            if (!isDisposed && e.State == CameraFocusState.Ongoing && State == CameraState.Preview) 
                StartCapture();
        }

        public void Scan(Action<Result> scanResultHandler)
        {
            resultHandler = scanResultHandler;
            StartFocusing(true);
        }
        public void ResumeAnalysis()
        {
            StartFocusing(true);
        }
        public void PauseAnalysis()
        {
            StopFocusing();
        }
        public void StopScanning()
        {            
            FocusStateChanged -= FocusStateChangedHandler;
            Capturing -= CapturingHandler;
            CaptureCompleted -= CaptureCompleteHandler;
            isDisposed = true;
            Dispose();
        }
        public void ToggleTorch()
        {
            torchMode = (Settings.FlashMode == CameraFlashMode.Off ? CameraFlashMode.On : CameraFlashMode.Off);
            if (State == CameraState.Preview)
                Settings.FlashMode = torchMode;             
            else
                torchFlag = true;
        }
        public void Torch(bool on)
        {
            torchMode = on ? CameraFlashMode.On : CameraFlashMode.Off;
            if (State == CameraState.Preview)
                Settings.FlashMode = torchMode;
            else
                torchFlag = true;

        }
        public void AutoFocus()
        {
            Settings.ClearFocusArea();
            Settings.AutoFocusMode = CameraAutoFocusMode.Normal;
        }
        public void AutoFocus(int x, int y)
        {
            AutoFocus();
            Settings.SetAutoFocusArea(x, y);
        }
    }
}
