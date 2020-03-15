using ElmSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Maps;

namespace ZXing.Mobile
{
	public partial class MobileBarcodeScanner
	{
		ZxingScannerWindow zxingScannerWindow;

		public Window MainWindow { get; internal set; }

		void PlatformInit()
		{
			zxingScannerWindow = new ZxingScannerWindow(this, OverlaySettings.WithView<Container>());
			MainWindow = zxingScannerWindow;
		}

		bool PlatformIsTorchOn => zxingScannerWindow.IsTorchOn;

		void PlatformAutoFocus()
			=> zxingScannerWindow?.AutoFocus();

		void PlatformCancel()
			=> zxingScannerWindow.Unrealize();

		void PlatformPauseAnalysis()
			=> zxingScannerWindow.PauseAnalysis();

		void PlatformResumeAnalysis()
			=> zxingScannerWindow.ResumeAnalysis();

		void PlatformScan(Action<Result[]> scanHandler)
		{
			zxingScannerWindow.OnBarcodeScanned +=
				(s, e) => scanHandler(e.Results);

			zxingScannerWindow.Show();
		}

		void PlatformToggleTorch()
			=> zxingScannerWindow?.ToggleTorch();

		void PlatformTorch(bool on)
			=> zxingScannerWindow?.Torch(on);
	}
}
