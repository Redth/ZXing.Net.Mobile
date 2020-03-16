using System;
using System.Linq;
using System.Threading.Tasks;
using ZXing.OneD;

namespace ZXing.UI
{
	public partial class BarcodeScanner : IBarcodeScanner
	{
		public BarcodeScanner(BarcodeScanningOptions options = null, BarcodeScannerOverlay overlay = null)
		{
			this.options = options ?? new BarcodeScanningOptions();
			this.overlay = overlay;
			Init();
		}

		internal TaskCompletionSource<Result[]> scanCompletionSource;

		void Init()
			=> PlatformInit();

		public Task<Result[]> ScanOnceAsync()
		{
			scanCompletionSource = new TaskCompletionSource<Result[]>();

			PlatformScan(async (r) =>
			{
				try
				{
					await CancelAsync();
				}
				catch { }

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

		public Task CancelAsync()
			=> PlatformCancelAsync();

		public Task AutoFocusAsync()
			=> PlatformAutoFocusAsync();

		public Task TorchAsync(bool on)
			=> PlatformTorchAsync(on);

		public Task ToggleTorchAsync()
			=> PlatformToggleTorchAsync();

		public bool IsTorchOn
			=> PlatformIsTorchOn;

		public bool IsAnalyzing
		{
			get => PlatformIsAnalyzing;
			set => PlatformIsAnalyzing = value;
		}

		readonly BarcodeScanningOptions options;
		public BarcodeScanningOptions Options => options;

		readonly BarcodeScannerOverlay overlay;
		public BarcodeScannerOverlay Overlay => overlay;

		public BarcodeScannerOverlay<TView> GetOverlay<TView>()
		{
			if (overlay == null)
				return null;

			if (overlay is BarcodeScannerOverlay<TView> vo)
				return vo;

			return overlay.WithView<TView>();
		}
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

		public static void Warn(Exception ex, string message)
			=> Log(LogLevel.Warn, message + Environment.NewLine + ex);

		public static void Error(string message)
			=> Log(LogLevel.Error, message);

		public static void Error(Exception ex)
			=> Log(LogLevel.Error, ex.ToString());

		public static void Error(Exception ex, string message)
			=> Log(LogLevel.Error, message + Environment.NewLine + ex);


		public static void Log(LogLevel logLevel, string message)
		{
			if ((int)logLevel <= (int)Level)
			{
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debug.WriteLine(message);

				Console.WriteLine(message);
			}
		}
	}

	public class BarcodeScannerOverlay<TView> : BarcodeScannerOverlay
	{
		public TView CustomOverlay { get; set; }
	}

	public class BarcodeScannerOverlay
	{
		public BarcodeScannerOverlay<TView> WithView<TView>()
			=> WithView<TView>(default);

		public BarcodeScannerOverlay<TView> WithView<TView>(TView customOverlay)
			=> new BarcodeScannerOverlay<TView>
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
