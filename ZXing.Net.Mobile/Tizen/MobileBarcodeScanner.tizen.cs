using ElmSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
	public partial class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		readonly ZxingScannerWindow zxingScannerWindow;

		public Container CustomOverlay { set; get; }

		public Window MainWindow { get; internal set; }

		public MobileBarcodeScanner() : base()
		{
			zxingScannerWindow = new ZxingScannerWindow();
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

		Task<Result> PlatformScan(MobileBarcodeScanningOptions options)
		{
			var task = Task.Factory.StartNew(() =>
			{
				var waitScanResetEvent = new ManualResetEvent(false);
				Result result = null;

				zxingScannerWindow.ScanningOptions = options;
				zxingScannerWindow.ScanContinuously = false;
				zxingScannerWindow.UseCustomOverlayView = UseCustomOverlay;
				zxingScannerWindow.CustomOverlayView = CustomOverlay;
				zxingScannerWindow.TopText = TopText;
				zxingScannerWindow.BottomText = BottomText;

				zxingScannerWindow.ScanCompletedHandler = (Result r) =>
				{
					result = r;
					waitScanResetEvent.Set();
				};
				zxingScannerWindow.Show();
				waitScanResetEvent.WaitOne();
				return result;
			});
			return task;
		}

		void PlatformScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
		{
			zxingScannerWindow.UseCustomOverlayView = UseCustomOverlay;
			zxingScannerWindow.CustomOverlayView = CustomOverlay;
			zxingScannerWindow.ScanningOptions = options;
			zxingScannerWindow.ScanContinuously = true;
			zxingScannerWindow.TopText = TopText;
			zxingScannerWindow.BottomText = BottomText;
			zxingScannerWindow.ScanCompletedHandler = (Result r) =>
			{
				scanHandler?.Invoke(r);
			};
			zxingScannerWindow.Show();
		}

		void PlatformToggleTorch()
			=> zxingScannerWindow?.ToggleTorch();

		void PlatformTorch(bool on)
			=> zxingScannerWindow?.Torch(on);
	}
}
