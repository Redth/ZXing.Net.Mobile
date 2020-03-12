using System;
using System.Linq;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace ZXing.Mobile
{
	class ZXingScannerCamera : Camera
	{
		Action<Result> resultHandler;
		bool isDisposed;
		bool torchFlag;
		CameraFlashMode torchMode;
		
		public MobileBarcodeScanningOptions ScanningOptions { get; set; }
		
		public bool IsTorchOn
		{
			get => Settings.FlashMode == CameraFlashMode.On;
			set => Settings.FlashMode = value ? CameraFlashMode.On : CameraFlashMode.Off;
		}

		public bool HasTorch
			=> Capabilities.SupportedFlashModes.ToList().Count > 0;

		public ZXingScannerCamera(CameraDevice device, MediaView mediaView) : base(device)
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

			if (State != CameraState.Preview)
				StartPreview();
		}

		void StateChangeHandler(object sender, CameraStateChangedEventArgs e)
		{
			if (!isDisposed && State == CameraState.Preview)
			{
				if (torchFlag)
					Settings.FlashMode = torchMode;
				torchFlag = false;
			}
		}

		async void CaptureCompleteHandler(object sender, EventArgs e)
		{
			if (!isDisposed)
			{
				if (ScanningOptions?.DelayBetweenContinuousScans > 0)
					await Task.Delay(ScanningOptions.DelayBetweenContinuousScans);
		
				StartPreview();
			}
		}

		async void CapturingHandler(object sender, CameraCapturingEventArgs e)
		{
			var result = await TizenBarcodeAnalyzer.AnalyzeBarcodeAsync(e.MainImage);
			
			if (result != null)
				resultHandler?.Invoke(result);
		}

		void FocusStateChangedHandler(object sender, CameraFocusStateChangedEventArgs e)
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
			=> StartFocusing(true);

		public void PauseAnalysis()
			=> StopFocusing();

		public void StopScanning()
		{
			FocusStateChanged -= FocusStateChangedHandler;
			Capturing -= CapturingHandler;
			CaptureCompleted -= CaptureCompleteHandler;
			StateChanged -= StateChangeHandler;
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
