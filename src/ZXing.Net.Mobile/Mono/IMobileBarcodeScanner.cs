using System;
using System.Threading.Tasks;
using ZXing;

namespace ZXing.Mobile
{

	public interface IMobileBarcodeScanner
	{
		Task<Result> Scan(MobileBarcodeScanningOptions options);
		Task<Result> Scan();

		void Cancel();

		void Torch(bool on);
		void AutoFocus();
		void ToggleTorch();

		bool UseCustomOverlay { get; }
		string TopText { get; set; }
		string BottomText { get; set; }

        string CancelButtonText { get; set; }
        string FlashButtonText { get; set; }
        string CameraUnsupportedMessage { get; set; }

		bool IsTorchOn { get; }
	}

	public abstract class MobileBarcodeScannerBase : IMobileBarcodeScanner
	{
        public MobileBarcodeScannerBase()
        {
            CancelButtonText = "Cancel";
            FlashButtonText = "Flash";
            CameraUnsupportedMessage = "Unable to start Camera for Scanning";
        }

		public bool UseCustomOverlay { get; set; }
		public string TopText { get; set; }
		public string BottomText { get; set; }
        public string CancelButtonText { get; set; }
        public string FlashButtonText { get; set; }
        public string CameraUnsupportedMessage { get; set; }

		public abstract Task<Result> Scan(MobileBarcodeScanningOptions options);

		public Task<Result> Scan()
		{
			return Scan(MobileBarcodeScanningOptions.Default);
		}

		public abstract void Cancel();

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