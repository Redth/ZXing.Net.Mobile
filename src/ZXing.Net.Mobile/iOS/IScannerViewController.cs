using System;
using MonoTouch.UIKit;

namespace ZXing.Mobile
{
	public interface IScannerViewController
	{
		void Torch(bool on);

		void ToggleTorch();
		void Cancel();

		bool IsTorchOn { get; }

		event Action<ZXing.Result> OnScannedResult;

		MobileBarcodeScanningOptions ScanningOptions { get;set; }
		MobileBarcodeScanner Scanner { get;set; }

		UIViewController AsViewController();
	}
}

