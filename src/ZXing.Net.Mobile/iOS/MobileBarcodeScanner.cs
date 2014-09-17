using System;
using System.Threading;
using System.Threading.Tasks;

#if __UNIFIED__
using Foundation;
using CoreFoundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
#endif

namespace ZXing.Mobile
{
	public class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		//ZxingCameraViewController viewController;
		IScannerViewController viewController;

		UIViewController appController;
		ManualResetEvent scanResultResetEvent = new ManualResetEvent(false);

		public MobileBarcodeScanner (UIViewController delegateController)
		{
			appController = delegateController;
		}

		public MobileBarcodeScanner ()
		{
			foreach (var window in UIApplication.SharedApplication.Windows)
			{
				if (window.RootViewController != null)
				{
					appController = window.RootViewController;
					break;
				}
			}
		}

		public Task<Result> Scan (bool useAVCaptureEngine)
		{
			return Scan (new MobileBarcodeScanningOptions (), useAVCaptureEngine);
		}


		public override Task<Result> Scan (MobileBarcodeScanningOptions options)
		{
			return Scan (options, false);
		}

		public Task<Result> Scan (MobileBarcodeScanningOptions options, bool useAVCaptureEngine)
		{
			return Task.Factory.StartNew(() => {

				try
				{
					scanResultResetEvent.Reset();

					Result result = null;

					Version sv = new Version (0, 0, 0);
					Version.TryParse (UIDevice.CurrentDevice.SystemVersion, out sv);

					var is7orgreater = sv.Major >= 7;
					var allRequestedFormatsSupported = true;

					if (useAVCaptureEngine)
						allRequestedFormatsSupported = AVCaptureScannerView.SupportsAllRequestedBarcodeFormats(options.PossibleFormats);

					this.appController.InvokeOnMainThread(() => {

										
						if (useAVCaptureEngine && is7orgreater && allRequestedFormatsSupported)
						{
							viewController = new AVCaptureScannerViewController(options, this);							
						}
						else
						{			
							if (useAVCaptureEngine && !is7orgreater)
								Console.WriteLine("Not iOS 7 or greater, cannot use AVCapture for barcode decoding, using ZXing instead");
							else if (useAVCaptureEngine && !allRequestedFormatsSupported)
								Console.WriteLine("Not all requested barcode formats were supported by AVCapture, using ZXing instead");

							viewController = new ZXing.Mobile.ZXingScannerViewController(options, this);
						}

						viewController.OnScannedResult += barcodeResult => {

							((UIViewController)viewController).InvokeOnMainThread(() => {
								viewController.Cancel();
								((UIViewController)viewController).DismissViewController(true, () => {

									result = barcodeResult;
									scanResultResetEvent.Set();

								});
							});
						};

						appController.PresentViewController((UIViewController)viewController, true, null);
					});

					scanResultResetEvent.WaitOne();
					((UIViewController)viewController).Dispose();

					return result;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					return null;
				}
			});

		}

		public override void Cancel ()
		{
			if (viewController != null)
			{
				((UIViewController)viewController).InvokeOnMainThread(() => {
					viewController.Cancel();

					((UIViewController)viewController).DismissViewController(true, null);
				});
			}

			scanResultResetEvent.Set();
		}

		public override void Torch (bool on)
		{
			if (viewController != null)
				viewController.Torch (on);
		}

		public override void ToggleTorch ()
		{
			viewController.ToggleTorch();
		}

		public override void AutoFocus ()
		{
			//Does nothing on iOS
		}

		public override bool IsTorchOn {
			get {
				return viewController.IsTorchOn;
			}
		}
		public UIView CustomOverlay { get;set; }

	}
}

