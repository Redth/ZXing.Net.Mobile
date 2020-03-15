using System;
using System.Threading.Tasks;

namespace ZXing.UI
{
	public interface IScannerView
	{
		event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		bool IsAnalyzing { get; set; }

		Task TorchAsync(bool on);
		Task AutoFocusAsync();
		Task AutoFocusAsync(int x, int y);
		Task ToggleTorchAsync();
		bool IsTorchOn { get; }
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
