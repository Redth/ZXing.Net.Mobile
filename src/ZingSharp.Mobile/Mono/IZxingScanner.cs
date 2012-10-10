using System;

namespace ZxingSharp.Mobile
{

	public interface IZxingScanner
	{
		void StartScanning(ZxingScanningOptions options, Action<ZxingBarcodeResult> onFinished);
		void StartScanning(Action<ZxingBarcodeResult> onFinished);
		void StopScanning();

		void Torch(bool on);
		void AutoFocus();
		void ToggleTorch();

		bool UseCustomOverlay { get; }
		string TopText { get; set; }
		string BottomText { get; set; }

		bool IsTorchOn { get; }


	}

	public abstract class ZxingScannerBase : IZxingScanner
	{
		public bool UseCustomOverlay { get; set; }
		public string TopText { get; set; }
		public string BottomText { get; set; }

		public abstract void StartScanning(ZxingScanningOptions options, Action<ZxingBarcodeResult> onFinished);

		public void StartScanning(Action<ZxingBarcodeResult> onFinished)
		{
			StartScanning(ZxingScanningOptions.Default, onFinished);
		}

		public abstract void StopScanning();

		public abstract void Torch(bool on);

		public abstract void ToggleTorch();

		public abstract bool IsTorchOn { get; }

		public abstract void AutoFocus();

	}

	public class CancelScanRequestEventArgs : EventArgs
	{
		public CancelScanRequestEventArgs ()
		{
			this.Cancel = false;
		}

		public bool Cancel { get; set; }
	}

}