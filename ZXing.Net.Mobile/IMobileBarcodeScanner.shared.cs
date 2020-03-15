using System;
using System.Linq;
using System.Threading.Tasks;
using ZXing;

namespace ZXing.Mobile
{
	public interface IMobileBarcodeScanner
	{
		Task<Result[]> ScanAsync();
		Task ScanContinuouslyAsync(Action<Result[]> scanHandler);

		void Cancel();

		void Torch(bool on);
		void AutoFocus();
		void ToggleTorch();

		void PauseAnalysis();
		void ResumeAnalysis();

		ScannerOverlaySettings OverlaySettings { get; }

		bool IsTorchOn { get; }
	}

	public class CancelScanRequestEventArgs : EventArgs
	{
		public CancelScanRequestEventArgs()
			=> Cancel = false;

		public bool Cancel { get; set; }
	}
}
