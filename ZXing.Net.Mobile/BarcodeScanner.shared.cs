using System;
using System.Threading.Tasks;

namespace ZXing.UI
{
	public partial class BarcodeScanner : IBarcodeScanner
	{
		public BarcodeScanner(BarcodeScannerSettings settings)
			: this(settings, null, null)
		{
		}

		public BarcodeScanner(BarcodeScannerSettings settings, BarcodeScannerDefaultOverlaySettings defaultOverlaySettings)
			: this(settings, defaultOverlaySettings, null)
		{
		}

		public BarcodeScanner(BarcodeScannerSettings settings, BarcodeScannerCustomOverlay customOverlay)
			: this(settings, null, customOverlay)
		{
		}

		public BarcodeScanner(BarcodeScannerSettings settings = null, BarcodeScannerDefaultOverlaySettings defaultOverlaySettings = null, BarcodeScannerCustomOverlay customOverlay = null)
		{
			this.settings = settings ?? new BarcodeScannerSettings();
			this.defaultOverlaySettings = defaultOverlaySettings;
			this.customOverlay = customOverlay;
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

		readonly BarcodeScannerSettings settings;
		public BarcodeScannerSettings Settings => settings;

		readonly BarcodeScannerDefaultOverlaySettings defaultOverlaySettings;
		public BarcodeScannerDefaultOverlaySettings DefaultOverlaySettings => defaultOverlaySettings;

		readonly BarcodeScannerCustomOverlay customOverlay;
		public BarcodeScannerCustomOverlay CustomOverlay => CustomOverlay;
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

	public partial class BarcodeScannerCustomOverlay
	{
		public BarcodeScannerCustomOverlay()
		{
		}
	}

	public class BarcodeScannerDefaultOverlaySettings
	{
		public string TopText { get; set; }

		public string BottomText { get; set; }

		public bool ShowFlashButton { get; set; }

		public bool ShowCancelButton { get; set; }
	}
}
