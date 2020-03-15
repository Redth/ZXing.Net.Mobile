using ElmSharp;
using System;

namespace ZXing.Mobile
{
	class ZxingScannerWindow : Window
	{
		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		public MobileBarcodeScanningOptions ScanningOptions
		{
			get => scanner?.ScanningOptions ?? options;
			set
			{
				if (scanner != null)
					scanner.ScanningOptions = value;
				else
					options = value;
			}
		}

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public ScannerOverlaySettings<Container> OverlaySettings { get; private set; }

		readonly MobileBarcodeScanner scanner;


		public bool IsTorchOn => zxingMediaView.IsTorchOn;

		ZXingMediaView zxingMediaView;
		Background overlayBackground;

		public ZxingScannerWindow()
			: this(null, null)
		{ }

		public ZxingScannerWindow(ScannerOverlaySettings<Container> overlaySettings)
			: this(null, overlaySettings)
		{ }

		public ZxingScannerWindow(MobileBarcodeScanner scanner, ScannerOverlaySettings<Container> overlaySettings) : base("ZXingScannerWindow")
		{
			this.scanner = scanner;
			OverlaySettings = overlaySettings;
			AvailableRotations = DisplayRotation.Degree_0 | DisplayRotation.Degree_180 | DisplayRotation.Degree_270 | DisplayRotation.Degree_90;
			BackButtonPressed += (s, ex) =>
			{
				zxingMediaView?.Dispose();
				Unrealize();
			};
			InitView();
			var showCallback = new EvasObjectEvent(this, EvasObjectCallbackType.Show);
			showCallback.On += (s, e) =>
			{
				if (OverlaySettings?.CustomOverlay != null)
				{
					overlayBackground.SetContent(OverlaySettings.CustomOverlay);
					OverlaySettings.CustomOverlay.Show();
				}
				else
				{
					var defaultOverlay = new ZXingDefaultOverlay(this);
					defaultOverlay.SetText(OverlaySettings?.TopText ?? string.Empty, OverlaySettings?.BottomText ?? string.Empty);
					overlayBackground.SetContent(defaultOverlay);
					defaultOverlay.Show();
				}
			};
		}

		void InitView()
		{
			var mBackground = new Background(this);
			mBackground.Show();

			var mConformant = new Conformant(this);
			mConformant.SetContent(mBackground);
			mConformant.Show();
			mBackground.Show();

			overlayBackground = new Background(this)
			{
				Color = Color.Transparent,
				BackgroundColor = Color.Transparent,
			};
			overlayBackground.Show();

			var oConformant = new Conformant(this);
			oConformant.Show();
			oConformant.SetContent(overlayBackground);

			zxingMediaView = new ZXingMediaView(this)
			{
				AlignmentX = -1,
				AlignmentY = -1,
				WeightX = 1,
				WeightY = 1,
			};
			zxingMediaView.Show();
			mBackground.SetContent(zxingMediaView);
		}


		public void AutoFocus()
			=> zxingMediaView?.AutoFocus();

		public void PauseAnalysis()
			=> zxingMediaView?.PauseAnalysis();

		public void ResumeAnalysis()
			=> zxingMediaView?.ResumeAnalysis();

		public void Torch(bool on)
			=> zxingMediaView?.Torch(on);

		public void ToggleTorch()
			=> zxingMediaView?.ToggleTorch();
	}
}