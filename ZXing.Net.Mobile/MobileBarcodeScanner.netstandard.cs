using System;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
	public partial class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		NotSupportedException ex = new NotSupportedException("MobileBarcodeScanner is unsupported on this platform.");

		Task<Result> PlatformScan(MobileBarcodeScanningOptions options)
			=> throw ex;

		void PlatformScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
			=> throw ex;

		void PlatformCancel()
			=> throw ex;

		void PlatformAutoFocus()
			=> throw ex;

		void PlatformTorch(bool on)
			=> throw ex;

		void PlatformToggleTorch()
			=> throw ex;

		void PlatformPauseAnalysis()
			=> throw ex;

		void PlatformResumeAnalysis()
			=> throw ex;

		bool PlatformIsTorchOn
			=> throw ex;
	}
}
