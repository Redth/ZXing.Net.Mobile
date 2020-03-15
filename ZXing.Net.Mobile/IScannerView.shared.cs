using System;

namespace ZXing.Mobile
{
	public interface IScannerView
	{
		event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		void PauseAnalysis();
		void ResumeAnalysis();

		void Torch(bool on);
		void AutoFocus();
		void AutoFocus(int x, int y);
		void ToggleTorch();
		bool IsTorchOn { get; }
		bool IsAnalyzing { get; }

		bool HasTorch { get; }
	}

	public class BarcodeScannedEventArgs : EventArgs
	{
		public BarcodeScannedEventArgs(ZXing.Result[] results)
			: base()
			=> Results = results;

		public BarcodeScannedEventArgs(ZXing.Result result)
			: base()
			=> Results = new[] { result };

		public ZXing.Result[] Results { get; private set; }
	}
}
