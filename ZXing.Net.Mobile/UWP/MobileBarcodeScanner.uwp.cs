using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ZXing.Mobile
{
	public partial class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		public MobileBarcodeScanner() : base()
		{
		}

		public MobileBarcodeScanner(CoreDispatcher dispatcher) : base()
		{
			Dispatcher = dispatcher;
		}

		internal ScanPage ScanPage { get; set; }

		public CoreDispatcher Dispatcher { get; set; }

		public Frame RootFrame { get; set; }

		async void PlatformScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
		{
			//Navigate: /ZxingSharp.WindowsPhone;component/Scan.xaml
			var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement)Window.Current.Content).GetFirstChildOfType<Frame>();
			var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

			await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				rootFrame.Navigate(typeof(ScanPage), new ScanPageNavigationParameters
				{
					Options = options,
					ResultHandler = scanHandler,
					Scanner = this,
					ContinuousScanning = true
				});
			});
		}

		async Task<Result> PlatformScan(MobileBarcodeScanningOptions options)
		{
			var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement)Window.Current.Content).GetFirstChildOfType<Frame>();
			var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

			var tcsScanResult = new TaskCompletionSource<Result>();

			await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var pageOptions = new ScanPageNavigationParameters
				{
					Options = options,
					ResultHandler = r =>
					{
						tcsScanResult.SetResult(r);
					},
					Scanner = this,
					ContinuousScanning = false,
					CameraInitialized = () => { OnCameraInitialized?.Invoke(); },
					CameraError = (errors) => { OnCameraError?.Invoke(errors); }
				};
				rootFrame.Navigate(typeof(ScanPage), pageOptions);
			});

			var result = await tcsScanResult.Task;

			await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				if (rootFrame.CanGoBack)
					rootFrame.GoBack();
			});

			return result;
		}

		public event ScannerOpened OnCameraInitialized;
		public delegate void ScannerOpened();

		public event ScannerError OnCameraError;
		public delegate void ScannerError(IEnumerable<string> errors);

		async void PlatformCancel()
		{
			var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement)Window.Current.Content).GetFirstChildOfType<Frame>();
			var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

			ScanPage?.Cancel();

			await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				if (rootFrame.CanGoBack)
					rootFrame.GoBack();
			});
		}

		void PlatformTorch(bool on)
			=> ScanPage?.Torch(on);

		void PlatformToggleTorch()
			=> ScanPage?.ToggleTorch();

		bool PlatformIsTorchOn
			=> ScanPage?.IsTorchOn ?? false;

		void PlatformAutoFocus()
			=> ScanPage?.AutoFocus();

		void PlatformPauseAnalysis()
			=> ScanPage?.PauseAnalysis();

		void PlatformResumeAnalysis()
			=> ScanPage?.ResumeAnalysis();

		public UIElement CustomOverlay { get; set; }

		internal static void Log(string message, params object[] args)
			=> System.Diagnostics.Debug.WriteLine(message, args);
	}
}
