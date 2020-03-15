using System;
using System.Linq;
using System.Threading.Tasks;
using ZXing;

namespace ZXing.UI
{
	public interface IBarcodeScanner
	{
		Task<Result[]> ScanOnceAsync();

		Task ScanContinuouslyAsync(Action<Result[]> scannedHandler);

		Task CancelAsync();

		Task TorchAsync(bool on);
		
		Task AutoFocusAsync();

		Task ToggleTorchAsync();

		BarcodeScannerOverlay Overlay { get; }

		bool IsTorchOn { get; }

		bool IsAnalyzing { get; set; }
	}
}
