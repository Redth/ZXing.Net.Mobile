using ElmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tizen.Multimedia;

namespace ZXing.Mobile
{
	public class ZXingMediaView : MediaView, IScannerView
	{
		ZXingScannerCamera zxingScannerCamera;
		EvasObjectEvent showCallback;
		public ZXingMediaView(EvasObject parent) : base(parent)
		{
			AlignmentX = -1;
			AlignmentY = -1;
			WeightX = 1;
			WeightY = 1;
			zxingScannerCamera = new ZXingScannerCamera(CameraDevice.Rear, this);

			showCallback = new EvasObjectEvent(this, EvasObjectCallbackType.Show);
			showCallback.On += (s, e) =>
			{
				if (zxingScannerCamera == null)
					zxingScannerCamera = new ZXingScannerCamera(CameraDevice.Rear, this);
			};

		}

		internal MobileBarcodeScanningOptions ScanningOptions
		{
			get => zxingScannerCamera?.ScanningOptions ?? new MobileBarcodeScanningOptions();
			set => zxingScannerCamera.ScanningOptions = value;
		}

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

		public void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
		{
			IsAnalyzing = true;
			Show();
			zxingScannerCamera.ScanningOptions = options;
			zxingScannerCamera?.Scan(scanResultHandler);
			IsAnalyzing = false;
		}

		public void StopScanning()
			=> zxingScannerCamera?.StopScanning();

		public void ToggleTorch()
			=> zxingScannerCamera?.ToggleTorch();

		public void Torch(bool on)
			=> zxingScannerCamera?.Torch(on);
	}
}