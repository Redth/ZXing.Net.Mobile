using ElmSharp;
using System;

namespace ZXing.Mobile
{
	class ZxingScannerWindow : Window
	{
		public Action<Result> ScanCompletedHandler { get; set; }
		public bool ScanContinuously { get; set; }

		public MobileBarcodeScanningOptions ScanningOptions {
			get => zxingMediaView?.ScanningOptions ?? new MobileBarcodeScanningOptions();
			set => zxingMediaView.ScanningOptions = value;
		}

		public bool IsTorchOn => zxingMediaView.IsTorchOn;

		public bool UseCustomOverlayView { get; set; }
		public Container CustomOverlayView { get; set; }
		public string TopText { get; internal set; }
		public string BottomText { get; internal set; }

		ZXingMediaView zxingMediaView;
		Background overlayBackground;

		public ZxingScannerWindow() : base("ZXingScannerWindow")
		{
			TopText = "";
			BottomText = "";
			AvailableRotations = DisplayRotation.Degree_0 | DisplayRotation.Degree_180 | DisplayRotation.Degree_270 | DisplayRotation.Degree_90;
			BackButtonPressed += (s, ex) =>
			{
				zxingMediaView?.StopScanning();
				Unrealize();
			};
			InitView();
			var showCallback = new EvasObjectEvent(this, EvasObjectCallbackType.Show);
			showCallback.On += (s, e) =>
			{
				StartScanning();
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

		public void StartScanning()
		{
			if (UseCustomOverlayView)
			{
				overlayBackground.SetContent(CustomOverlayView);
				CustomOverlayView.Show();
			}
			else
			{
				var defaultOverlay = new ZXingDefaultOverlay(this);
				defaultOverlay.SetText(TopText, BottomText);
				overlayBackground.SetContent(defaultOverlay);
				defaultOverlay.Show();
			}
			zxingMediaView.StartScanning(result =>
			{
				ScanCompletedHandler?.Invoke(result);
				if (!ScanContinuously)
				{
					zxingMediaView.StopScanning();
					Unrealize();
				}
			}, ScanningOptions);
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