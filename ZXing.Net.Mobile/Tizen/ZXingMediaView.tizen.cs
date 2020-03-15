using ElmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tizen.Multimedia;

namespace ZXing.UI
{
	public class ZXingMediaView : MediaView, IScannerView, IDisposable
	{
		ZXingScannerCamera zxingScannerCamera;
		EvasObjectEvent showCallback;

		public BarcodeScanningOptions Options { get; }

		public ZXingMediaView(EvasObject parent, BarcodeScanningOptions options) : base(parent)
		{
			Options = options ?? new BarcodeScanningOptions();

			AlignmentX = -1;
			AlignmentY = -1;
			WeightX = 1;
			WeightY = 1;

			zxingScannerCamera = new ZXingScannerCamera(CameraDevice.Rear, this, Options);

			showCallback = new EvasObjectEvent(this, EvasObjectCallbackType.Show);
			showCallback.On += (s, e) =>
			{
				if (zxingScannerCamera == null)
					zxingScannerCamera = new ZXingScannerCamera(CameraDevice.Rear, this);
			};

			StartScanning();
		}

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public bool IsTorchOn => zxingScannerCamera.IsTorchOn;

		public bool HasTorch => zxingScannerCamera.HasTorch;

		public bool IsAnalyzing
		{
			get => zxingScannerCamera?.IsAnalyzing ?? false;
			set { if (zxingScannerCamera != null) zxingScannerCamera.IsAnalyzing = value; }
		}

		public Task AutoFocusAsync()
			=> zxingScannerCamera?.AutoFocusAsync();

		public Task AutoFocusAsync(int x, int y)
			=> zxingScannerCamera?.AutoFocusAsync(x, y);


		void StartScanning()
		{
			IsAnalyzing = true;
			Show();
			zxingScannerCamera.OnBarcodeScanned += OnBarcodeScanned;
			IsAnalyzing = false;
		}

		public Task ToggleTorchAsync()
			=> zxingScannerCamera?.ToggleTorchAsync();

		public Task TorchAsync(bool on)
			=> zxingScannerCamera?.TorchAsync(on);

		public void Dispose()
			=> zxingScannerCamera?.Dispose();
	}
}