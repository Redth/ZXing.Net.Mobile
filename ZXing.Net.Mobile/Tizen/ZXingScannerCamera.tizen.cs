using System;
using System.Linq;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace ZXing.UI
{
	class ZXingScannerCamera : Camera, IScannerView, IDisposable
	{
		bool isDisposed;
		bool torchFlag;
		CameraFlashMode torchMode;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public new BarcodeScannerSettings Settings { get; }

		public bool IsTorchOn
		{
			get => base.Settings.FlashMode == CameraFlashMode.On;
			set => base.Settings.FlashMode = value ? CameraFlashMode.On : CameraFlashMode.Off;
		}

		public bool HasTorch
			=> Capabilities.SupportedFlashModes.ToList().Count > 0;

		bool isAnalyzing = false;
		public bool IsAnalyzing
		{
			get => isAnalyzing;
			set
			{
				isAnalyzing = value;
				if (isAnalyzing)
					StartFocusing(true);
				else
					StopFocusing();
			}
		}

		public ZXingScannerCamera(CameraDevice device, MediaView mediaView)
			: this(device, mediaView, null)
		{ }

		internal ZXingScannerCamera(CameraDevice device, MediaView mediaView, BarcodeScannerSettings settings) : base(device)
		{
			Settings = settings ?? new BarcodeScannerSettings();
			Display = new Display(mediaView);
			base.Settings.ImageQuality = 100;
			base.Settings.PreviewPixelFormat = Capabilities.SupportedPreviewPixelFormats.FirstOrDefault();
			base.Settings.PreviewResolution = base.Settings.RecommendedPreviewResolution;
			base.Settings.CapturePixelFormat = CameraPixelFormat.Nv12;
			base.Settings.CaptureResolution = Capabilities.SupportedCaptureResolutions.FirstOrDefault();
			base.Settings.ClearFocusArea();
			base.Settings.AutoFocusMode = CameraAutoFocusMode.Normal;

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
					base.Settings.FlashMode = torchMode;
				torchFlag = false;
			}
		}

		async void CaptureCompleteHandler(object sender, EventArgs e)
		{
			if (!isDisposed)
			{
				if (Settings?.DelayBetweenContinuousScans > TimeSpan.Zero)
					await Task.Delay(Settings.DelayBetweenContinuousScans);

				StartPreview();
			}
		}

		async void CapturingHandler(object sender, CameraCapturingEventArgs e)
		{
			if (!IsAnalyzing)
				return;

			var result = await TizenBarcodeAnalyzer.AnalyzeBarcodeAsync(e.MainImage, Settings?.DecodeMultipleBarcodes ?? false);

			if (result != null && result.Length > 0)
			{
				var filteredResults = result.Where(r => r != null && !string.IsNullOrWhiteSpace(r.Text)).ToArray();

				if (filteredResults.Any())
					OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(filteredResults));
			}
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

		public Task ToggleTorchAsync()
		{
            torchMode = (base.Settings.FlashMode == CameraFlashMode.Off ? CameraFlashMode.On : CameraFlashMode.Off);
			if (State == CameraState.Preview)
				base.Settings.FlashMode = torchMode;
			else
				torchFlag = true;

			return Task.CompletedTask;
		}

		public Task TorchAsync(bool on)
		{
			torchMode = on ? CameraFlashMode.On : CameraFlashMode.Off;
			if (State == CameraState.Preview)
				base.Settings.FlashMode = torchMode;
			else
				torchFlag = true;

			return Task.CompletedTask;
		}

		public Task AutoFocusAsync()
		{
			base.Settings.ClearFocusArea();
			base.Settings.AutoFocusMode = CameraAutoFocusMode.Normal;

			return Task.CompletedTask;
		}

		public async Task AutoFocusAsync(int x, int y)
		{
			await AutoFocusAsync();
			base.Settings.SetAutoFocusArea(x, y);
		}
	}
}
