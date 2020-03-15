using System;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using CoreFoundation;
using UIKit;

namespace ZXing.Mobile
{
	public partial class MobileBarcodeScanner
	{
		IScannerViewController viewController;
		readonly WeakReference<UIViewController> weakAppController;
		TaskCompletionSource<Result[]> tcsScan;

		public MobileBarcodeScanner(UIViewController delegateController)
			=> weakAppController = new WeakReference<UIViewController>(delegateController);

		public MobileBarcodeScanner()
			=> weakAppController = new WeakReference<UIViewController>(Xamarin.Essentials.Platform.GetCurrentUIViewController());

		void PlatformInit()
		{ }

		internal void PlatformScan(Action<Result[]> scanHandler)
		{
			var useAVCaptureEngine = ScanningOptions?.UseNativeScanning ?? false;

			try
			{
				var sv = new Version(0, 0, 0);
				Version.TryParse(UIDevice.CurrentDevice.SystemVersion, out sv);

				var is7orgreater = sv.Major >= 7;
				var allRequestedFormatsSupported = true;

				if (useAVCaptureEngine)
					allRequestedFormatsSupported = AVCaptureScannerView.SupportsAllRequestedBarcodeFormats(ScanningOptions.PossibleFormats);

				if (weakAppController.TryGetTarget(out var appController))
				{
					var tcs = new TaskCompletionSource<object>();

					appController?.InvokeOnMainThread(() =>
					{
						if (useAVCaptureEngine && is7orgreater && allRequestedFormatsSupported)
						{
							viewController = new AVCaptureScannerViewController(this, OverlaySettings.WithView<UIView>());
						}
						else
						{
							if (useAVCaptureEngine && !is7orgreater)
								Console.WriteLine("Not iOS 7 or greater, cannot use AVCapture for barcode decoding, using ZXing instead");
							else if (useAVCaptureEngine && !allRequestedFormatsSupported)
								Console.WriteLine("Not all requested barcode formats were supported by AVCapture, using ZXing instead");

							viewController = new ZXing.Mobile.ZXingScannerViewController(this, OverlaySettings.WithView<UIView>());
						}

						viewController.OnBarcodeScanned += (s, e) => scanHandler(e.Results);
						appController?.PresentViewController((UIViewController)viewController, true, null);
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		void PlatformCancel()
		{
			if (viewController != null)
			{
				((UIViewController)viewController).InvokeOnMainThread(() =>
				{
					viewController.Cancel();

					// Calling with animated:true here will result in a blank screen when the scanner is closed on iOS 7.
					((UIViewController)viewController).DismissViewController(true, null);
				});
			}
		}

		void PlatformTorch(bool on)
			=> viewController?.Torch(on);

		void PlatformToggleTorch()
			=> viewController?.ToggleTorch();

		void PlatformAutoFocus()
			=> viewController?.AutoFocus();

		void PlatformPauseAnalysis()
			=> viewController?.PauseAnalysis();

		void PlatformResumeAnalysis()
			=> viewController?.ResumeAnalysis();

		bool PlatformIsTorchOn
			=> viewController.IsTorchOn;

		public UIView CustomOverlay { get; set; }
	}
}
