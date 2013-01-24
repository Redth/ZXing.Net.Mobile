using System;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;

namespace ZXing.Mobile
{
	public class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		ZxingCameraViewController viewController;
		UIViewController appController;

		public MobileBarcodeScanner (object delegateController)
		{
			appController = (UIViewController)delegateController;
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
					var scanResultResetEvent = new System.Threading.ManualResetEvent(false);
					Result result = null;

					this.appController.InvokeOnMainThread(() => {
						// Free memory first and release resources
						if (viewController != null)
						{
							viewController.Dispose();
							viewController = null;
						}

						viewController = new ZxingCameraViewController(options, this);

						viewController.BarCodeEvent += (BarCodeEventArgs e) => {

							viewController.DismissViewController();

							result = e.BarcodeResult;
							scanResultResetEvent.Set();

						};

						viewController.Canceled += (sender, e) => {

							viewController.DismissViewController();

							scanResultResetEvent.Set();
						};

						appController.PresentViewController(viewController, true, () => { });

					});

					scanResultResetEvent.WaitOne();

					return result;
				}
				catch (Exception ex)
				{
					return null;
				}
			});

		}

		public override void Cancel ()
		{
			if (viewController != null)
				viewController.DismissViewController();
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

