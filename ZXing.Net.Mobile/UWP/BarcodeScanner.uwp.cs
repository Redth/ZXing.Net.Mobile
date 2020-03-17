using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ZXing.UI
{
	public partial class BarcodeScanner
	{
		public CoreDispatcher Dispatcher { get; set; }

		public Frame RootFrame { get; set; }

		ScanPageNavigationParameters navigationParameters;

		void PlatformInit()
		{ }

		async void PlatformScan(Action<ZXing.Result[]> scanHandler)
		{
			var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement)Window.Current.Content).GetFirstChildOfType<Frame>();
			var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

			await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				navigationParameters = new ScanPageNavigationParameters(Settings, DefaultOverlaySettings, CustomOverlay);
				navigationParameters.BarcodeScannedHandler = r => scanHandler(r);
				rootFrame.Navigate(typeof(ZXingScannerPage), navigationParameters);
			});
		}

		async Task PlatformCancelAsync()
		{
			var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement)Window.Current.Content).GetFirstChildOfType<Frame>();
			var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

			await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				if (rootFrame.CanGoBack)
					rootFrame.GoBack();
			});
		}

		Task PlatformTorchAsync(bool on)
			=> navigationParameters?.TorchHandler(on);

		Task PlatformToggleTorchAsync()
			=> navigationParameters?.ToggleTorchHandler();

		bool PlatformIsTorchOn
			=> navigationParameters?.IsTorchOnHandler() ?? false;

		Task PlatformAutoFocusAsync()
			=> navigationParameters?.AutoFocusHandler();

		Task PlatformAutoFocusAsync(int x, int y)
			=> navigationParameters?.AutoFocusXYHandler(x, y);

		bool PlatformIsAnalyzing
		{
			get => navigationParameters?.IsAnalyzingGetHandler() ?? false;
			set => navigationParameters?.IsAnalyzingSetHandler(value);
		}
	}

	public partial class BarcodeScannerCustomOverlay
	{
		public BarcodeScannerCustomOverlay(UIElement nativeView)
			=> NativeView = nativeView;

		public readonly UIElement NativeView;
	}
}
