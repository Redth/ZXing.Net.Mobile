using System;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using CoreFoundation;
using UIKit;

namespace ZXing.UI
{
	public partial class BarcodeScanner
	{
		IScannerView viewController;

		WeakReference<UIViewController> weakAppController;
		TaskCompletionSource<Result[]> tcsScan;

		void PlatformInit()
		{
			weakAppController = new WeakReference<UIViewController>(Xamarin.Essentials.Platform.GetCurrentUIViewController());
		}

		internal void PlatformScan(Action<Result[]> scanHandler)
		{
			var useAVCaptureEngine = Settings?.UseNativeScanning ?? false;

			try
			{
				var sv = new Version(0, 0, 0);
				Version.TryParse(UIDevice.CurrentDevice.SystemVersion, out sv);

				var is7orgreater = sv.Major >= 7;
				var allRequestedFormatsSupported = true;

				if (useAVCaptureEngine)
					allRequestedFormatsSupported = AVCaptureScannerView.SupportsAllRequestedBarcodeFormats(Settings.DecodingOptions.PossibleFormats);

				if (weakAppController.TryGetTarget(out var appController))
				{
					var tcs = new TaskCompletionSource<object>();

					appController?.InvokeOnMainThread(() =>
					{
						if (useAVCaptureEngine && is7orgreater && allRequestedFormatsSupported)
						{
							viewController = new AVCaptureScannerViewController(Settings, DefaultOverlaySettings, CustomOverlay);
						}
						else
						{
							if (useAVCaptureEngine && !is7orgreater)
								Logger.Error("Not iOS 7 or greater, cannot use AVCapture for barcode decoding, using ZXing instead");
							else if (useAVCaptureEngine && !allRequestedFormatsSupported)
								Logger.Error("Not all requested barcode formats were supported by AVCapture, using ZXing instead");

							viewController = new ZXingScannerViewController(Settings, DefaultOverlaySettings, CustomOverlay);
						}

						viewController.OnBarcodeScanned += (s, e) => scanHandler(e.Results);
						appController?.PresentViewController((UIViewController)viewController, true, null);
					});
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
			}
		}

		Task PlatformCancelAsync()
		{
			if (viewController != null)
			{
				((UIViewController)viewController).InvokeOnMainThread(() =>
				{
					// TODO: We might need to shut things down here

					// Calling with animated:true here will result in a blank screen when the scanner is closed on iOS 7.
					((UIViewController)viewController).DismissViewController(true, null);
				});
			}

			return Task.CompletedTask;
		}

		Task PlatformTorchAsync(bool on)
			=> viewController?.TorchAsync(on);

		Task PlatformToggleTorchAsync()
			=> viewController?.ToggleTorchAsync();

		Task PlatformAutoFocusAsync()
			=> viewController?.AutoFocusAsync();


		bool PlatformIsAnalyzing
		{
			get => viewController?.IsAnalyzing ?? false;
			set { if (viewController != null) viewController.IsAnalyzing = value; }
		}

		bool PlatformIsTorchOn
			=> viewController.IsTorchOn;
	}

	public partial class BarcodeScannerCustomOverlay
	{
		public BarcodeScannerCustomOverlay(UIView nativeView)
			=> NativeView = nativeView;

		public readonly UIView NativeView;
	}
}
