using System;
using System.Linq;
using System.Threading.Tasks;
using ZXing.OneD;

namespace ZXing.Mobile
{
	public partial class MobileBarcodeScanner : IMobileBarcodeScanner
	{
		public MobileBarcodeScanner(MobileBarcodeScanningOptions options)
			: this(options, null)
		{
		}

		public MobileBarcodeScanner(ScannerOverlaySettings overlaySettings)
			: this(null, overlaySettings)
		{
		}

		public MobileBarcodeScanner(MobileBarcodeScanningOptions options, ScannerOverlaySettings overlaySettings)
		{
			ScanningOptions = options ?? new MobileBarcodeScanningOptions();
			OverlaySettings = overlaySettings;
			Init();
		}

		internal TaskCompletionSource<Result[]> scanCompletionSource;

		void Init()
			=> PlatformInit();

		public Task<Result[]> ScanAsync()
		{
			scanCompletionSource = new TaskCompletionSource<Result[]>();

			PlatformScan(r =>
			{
				Cancel();
				scanCompletionSource.TrySetResult(r);
			});

			return scanCompletionSource.Task;
		}

		public Task ScanContinuouslyAsync(Action<Result[]> scanHandler)
		{
			scanCompletionSource = new TaskCompletionSource<Result[]>();

			PlatformScan(scanHandler);

			return scanCompletionSource.Task;
		}

		public void Cancel()
			=> PlatformCancel();

		public void AutoFocus()
			=> PlatformAutoFocus();

		public void Torch(bool on)
			=> PlatformTorch(on);

		public void ToggleTorch()
			=> PlatformToggleTorch();

		public void PauseAnalysis()
			=> PlatformPauseAnalysis();

		public void ResumeAnalysis()
			=> PlatformResumeAnalysis();

		public bool IsTorchOn
			=> PlatformIsTorchOn;

		public MobileBarcodeScanningOptions ScanningOptions { get; set; } = new MobileBarcodeScanningOptions();

		public ScannerOverlaySettings OverlaySettings { get; private set; }
	}

	public enum LogLevel
	{
		Error = 0,
		Warn = 1,
		Info = 2
	}

	public static class Logger
	{
		public static LogLevel Level { get; set; } = LogLevel.Warn;

		public static void Info(string message)
			=> Log(LogLevel.Info, message);

		public static void Warn(string message)
			=> Log(LogLevel.Warn, message);

		public static void Error(string message)
			=> Log(LogLevel.Error, message);

		public static void Error(Exception ex)
			=> Log(LogLevel.Error, ex.ToString());

		public static void Error(Exception ex, string message)
			=> Log(LogLevel.Error, message + Environment.NewLine + ex);


		public static void Log(LogLevel logLevel, string message)
		{
			if ((int)logLevel <= (int)Level)
				Console.WriteLine(message);
		}
	}

	public class ScannerOverlaySettings<TView> : ScannerOverlaySettings
	{
		public TView CustomOverlay { get; set; }
	}

	public class ScannerOverlaySettings
	{
		public ScannerOverlaySettings<TView> WithView<TView>()
			=> WithView<TView>(default);

		public ScannerOverlaySettings<TView> WithView<TView>(TView customOverlay)
			=> new ScannerOverlaySettings<TView>
			{
				CustomOverlay = customOverlay,
				TopText = this.TopText,
				BottomText = this.BottomText,
				FlashButtonText = this.FlashButtonText,
				CancelButtonText = this.CancelButtonText
			};

		public string TopText { get; set; }

		public string BottomText { get; set; }

		public string FlashButtonText { get; set; }

		public string CancelButtonText { get; set; }
	}
}
