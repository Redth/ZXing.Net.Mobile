using System;

using UIKit;

namespace ZXing.Mobile
{
	public interface IScannerViewController
	{
		void Torch(bool on);

		void ToggleTorch();
		void Cancel();

		bool IsTorchOn { get; }
		bool ContinuousScanning { get; set; }

		void PauseAnalysis();
		void ResumeAnalysis();

		event Action<IScanResult> OnScannedResult;

		MobileBarcodeScanningOptions ScanningOptions { get; set; }
		MobileBarcodeScanner Scanner { get; set; }

		UIViewController AsViewController();
	}
}

