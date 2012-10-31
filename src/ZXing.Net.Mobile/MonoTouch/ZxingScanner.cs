using System;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;

namespace ZxingSharp.Mobile
{
	public class ZxingScanner : ZxingScannerBase
	{
		ZxingCameraViewController viewController;
		UIViewController appController;

		public ZxingScanner ()
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

		public override void StartScanning (ZxingScanningOptions options, Action<ZxingBarcodeResult> onFinished)
		{

			viewController = new ZxingCameraViewController(options, this);

			viewController.BarCodeEvent += (BarCodeEventArgs e) => {

				viewController.DismissViewController();

				ZxingBarcodeResult result = null;

				if (e.BarcodeResult != null)
					result = ZxingBarcodeResult.FromZxingResult(e.BarcodeResult);

				if (onFinished != null)
					onFinished(result);
			};

			viewController.Canceled += (sender, e) => {

				viewController.DismissViewController();

				if (onFinished != null)
					onFinished(null);
			};

			appController.PresentViewController(viewController, true, () => { });
		}

		public override void StopScanning ()
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

