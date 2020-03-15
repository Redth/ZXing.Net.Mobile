using System;
using System.Linq;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace ZXing.Mobile
{
	class ZXingScannerCamera : Camera, IScannerView, IDisposable
	{
		Action<Result> resultHandler;
		bool isDisposed;
		bool torchFlag;
		CameraFlashMode torchMode;

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		public MobileBarcodeScanningOptions ScanningOptions
		{
			get => parentView?.ScanningOptions ?? options;
			set
			{
				if (parentView != null)
					parentView.ScanningOptions = value;
				else
					options = value;
			}
		}

		ZXingMediaView parentView;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public bool IsTorchOn
		{
			get => Settings.FlashMode == CameraFlashMode.On;
			set => Settings.FlashMode = value ? CameraFlashMode.On : CameraFlashMode.Off;
		}

		public bool HasTorch
			=> Capabilities.SupportedFlashModes.ToList().Count > 0;

		public bool IsAnalyzing { get; set; }

		public ZXingScannerCamera(CameraDevice device, MediaView mediaView)
			: this(device, mediaView, null)
		{ }

		internal ZXingScannerCamera(CameraDevice device, MediaView mediaView, ZXingMediaView parentView) : base(device)
		{
			this.parentView = parentView;
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
			if (!IsAnalyzing)
				return;

			var result = await TizenBarcodeAnalyzer.AnalyzeBarcodeAsync(e.MainImage, ScanningOptions?.ScanMultiple ?? false);

			if (result != null && result.Length > 0 && result[0] != null)
				OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(result));
		}

		void FocusStateChangedHandler(object sender, CameraFocusStateChangedEventArgs e)
		{
			if (!isDisposed && e.State == CameraFocusState.Ongoing && State == CameraState.Preview)
				StartCapture();
		}

		public void ResumeAnalysis()
			=> StartFocusing(true);

		public void PauseAnalysis()
			=> StopFocusing();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				FocusStateChanged -= FocusStateChangedHandler;
				Capturing -= CapturingHandler;
				CaptureCompleted -= CaptureCompleteHandler;
				StateChanged -= StateChangeHandler;
				isDisposed = true;
			}
			base.Dispose(disposing);
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
