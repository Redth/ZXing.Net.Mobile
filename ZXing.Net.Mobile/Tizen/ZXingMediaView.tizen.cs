using ElmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tizen.Multimedia;

namespace ZXing.Mobile
{
	public class ZXingMediaView : MediaView, IScannerView, IDisposable
	{
		ZXingScannerCamera zxingScannerCamera;
		EvasObjectEvent showCallback;

		public ZXingMediaView(EvasObject parent)
			: this(parent, null)
		{
		}

		internal ZXingMediaView(EvasObject parent, ZxingScannerWindow parentWindow) : base(parent)
		{
			AlignmentX = -1;
			AlignmentY = -1;
			WeightX = 1;
			WeightY = 1;
			this.parentWindow = parentWindow;
			zxingScannerCamera = new ZXingScannerCamera(CameraDevice.Rear, this);

			showCallback = new EvasObjectEvent(this, EvasObjectCallbackType.Show);
			showCallback.On += (s, e) =>
			{
				if (zxingScannerCamera == null)
					zxingScannerCamera = new ZXingScannerCamera(CameraDevice.Rear, this);
			};

			StartScanning();
		}

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		public MobileBarcodeScanningOptions ScanningOptions
		{
			get => parentWindow?.ScanningOptions ?? options;
			set
			{
				if (parentWindow != null)
					parentWindow.ScanningOptions = value;
				else
					options = value;
			}
		}

		ZxingScannerWindow parentWindow;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public ScannerOverlaySettings<Container> OverlaySettings { get; private set; }

		public bool IsTorchOn => zxingScannerCamera.IsTorchOn;

		public bool IsAnalyzing { get; private set; }

		public bool HasTorch => zxingScannerCamera.HasTorch;

		public void AutoFocus()
			=> zxingScannerCamera?.AutoFocus();

		public void AutoFocus(int x, int y)
			=> zxingScannerCamera?.AutoFocus(x, y);

		public void PauseAnalysis()
			=> zxingScannerCamera?.PauseAnalysis();

		public void ResumeAnalysis()
			=> zxingScannerCamera?.ResumeAnalysis();

		void StartScanning()
		{
			IsAnalyzing = true;
			Show();
			zxingScannerCamera.ScanningOptions = options;
			zxingScannerCamera.OnBarcodeScanned += OnBarcodeScanned;
			IsAnalyzing = false;
		}

		public void ToggleTorch()
			=> zxingScannerCamera?.ToggleTorch();

		public void Torch(bool on)
			=> zxingScannerCamera?.Torch(on);

		public void Dispose()
		{
			zxingScannerCamera?.Dispose();
		}
	}
}