using ElmSharp;
using System;
using System.Threading.Tasks;

namespace ZXing.UI
{
	public class ZxingScannerWindow : Window, IScannerView
	{
		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public BarcodeScanningOptions Options { get; }

		public BarcodeScannerOverlay<Container> Overlay { get; }

		ZXingMediaView zxingMediaView;
		Background overlayBackground;

		public ZxingScannerWindow()
			: this(null, null)
		{ }

		public ZxingScannerWindow(BarcodeScannerOverlay<Container> overlaySettings)
			: this(null, overlaySettings)
		{ }

		public ZxingScannerWindow(BarcodeScanningOptions options = null, BarcodeScannerOverlay<Container> overlay = null) : base("ZXingScannerWindow")
		{
			Options = options ?? new BarcodeScanningOptions();
			Overlay = overlay;
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
				if (Overlay?.CustomOverlay != null)
				{
					overlayBackground.SetContent(Overlay.CustomOverlay);
					Overlay.CustomOverlay.Show();
				}
				else
				{
					var defaultOverlay = new ZXingDefaultOverlay(this);
					defaultOverlay.SetText(Overlay?.TopText ?? string.Empty, Overlay?.BottomText ?? string.Empty);
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

			zxingMediaView = new ZXingMediaView(this, Options)
			{
				AlignmentX = -1,
				AlignmentY = -1,
				WeightX = 1,
				WeightY = 1,
			};
			zxingMediaView.Show();
			mBackground.SetContent(zxingMediaView);
		}

		public bool IsAnalyzing
		{
			get => zxingMediaView?.IsAnalyzing ?? false;
			set { if (zxingMediaView != null) zxingMediaView.IsAnalyzing = value; }
		}

		public bool IsTorchOn => zxingMediaView.IsTorchOn;

		public bool HasTorch => zxingMediaView.HasTorch;

		public Task AutoFocusAsync()
			=> zxingMediaView?.AutoFocusAsync();

		public Task AutoFocusAsync(int x, int y)
			=> zxingMediaView?.AutoFocusAsync(x, y);

		public Task TorchAsync(bool on)
			=> zxingMediaView?.TorchAsync(on);

		public Task ToggleTorchAsync()
			=> zxingMediaView?.ToggleTorchAsync();
	}
}