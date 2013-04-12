using System;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using System.Threading;

namespace ZXing.Mobile
{
	public class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		//ZxingCameraViewController viewController;
		ZXingScannerViewController viewController;
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

		public override Task<Result> Scan (MobileBarcodeScanningOptions options)
		{
			return Task.Factory.StartNew(() => {

				try
				{
					scanResultResetEvent.Reset();

					Result result = null;

					this.appController.InvokeOnMainThread(() => {

						//viewController = new ZxingCameraViewController(options, this);
						viewController = new ZXing.Mobile.ZXingScannerViewController(options, this);

						viewController.OnScannedResult += barcodeResult => {

							viewController.InvokeOnMainThread(() => {
								viewController.Cancel();
								viewController.DismissViewController(true, null);
							});

							result = barcodeResult;
							scanResultResetEvent.Set();
						};

						appController.PresentViewController(viewController, true, null);
					});

					scanResultResetEvent.WaitOne();
					viewController.Dispose();

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
				viewController.InvokeOnMainThread(() => {
					viewController.Cancel();

					viewController.DismissViewController(true, null);
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

