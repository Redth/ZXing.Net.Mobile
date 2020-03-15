using System;
using System.Threading.Tasks;

namespace ZXing.UI
{
	public partial class BarcodeScanner
	{
		NotSupportedException ex = new NotSupportedException("BarcodeScanner is unsupported on this platform.");

		void PlatformInit()
			=> throw ex;

		void PlatformScan(Action<ZXing.Result[]> scanHandler)
			=> throw ex;

		Task PlatformCancelAsync()
			=> throw ex;

		Task PlatformAutoFocusAsync()
			=> throw ex;

		Task PlatformTorchAsync(bool on)
			=> throw ex;

		Task PlatformToggleTorchAsync()
			=> throw ex;

		bool PlatformIsTorchOn
			=> throw ex;

		bool PlatformIsAnalyzing
		{
			get => throw ex;
			set => throw ex;
		}
	}
}
