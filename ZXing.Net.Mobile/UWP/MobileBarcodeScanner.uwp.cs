using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ZXing.Mobile
{
	public partial class MobileBarcodeScanner
	{
		internal ScanPage ScanPage { get; set; }

		public CoreDispatcher Dispatcher { get; set; }

		public Frame RootFrame { get; set; }

		void PlatformInit()
		{ }

		async void PlatformScan(Action<ZXing.Result[]> scanHandler)
		{
			var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement)Window.Current.Content).GetFirstChildOfType<Frame>();
			var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

			await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var pageOptions = new ScanPageNavigationParameters
				{
					Scanner = this,
					OverlaySettings = this.OverlaySettings.WithView<UIElement>()
				};
				rootFrame.Navigate(typeof(ScanPage), pageOptions);
			});
		}

		async void PlatformCancel()
		{
			var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement)Window.Current.Content).GetFirstChildOfType<Frame>();
			var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

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
