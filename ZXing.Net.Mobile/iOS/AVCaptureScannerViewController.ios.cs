using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

using UIKit;
using Foundation;
using AVFoundation;
using CoreGraphics;

using ZXing;
using System.Threading.Tasks;

namespace ZXing.UI
{
	public class AVCaptureScannerViewController : UIViewController, IScannerView
	{
		AVCaptureScannerView scannerView;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public BarcodeScannerSettings Settings { get; }

		public BarcodeScannerCustomOverlay CustomOverlay { get; }

		public BarcodeScannerDefaultOverlaySettings DefaultOverlaySettings { get; }

		UIActivityIndicatorView loadingView;
		UIView loadingBg;

		public AVCaptureScannerViewController(BarcodeScannerSettings options = null, BarcodeScannerDefaultOverlaySettings defaultOverlaySettings = null, BarcodeScannerCustomOverlay customOverlay = null)
		{
			Settings = options;
			CustomOverlay = customOverlay;
			DefaultOverlaySettings = defaultOverlaySettings;

			var appFrame = UIScreen.MainScreen.ApplicationFrame;

			View.Frame = new CGRect(0, 0, appFrame.Width, appFrame.Height);
			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		public UIViewController AsViewController()
			=> this;

		UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewDidLoad()
		{
			loadingBg = new UIView(this.View.Frame)
			{
				BackgroundColor = UIColor.Black,
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
			};
			loadingView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleMargins
			};
			loadingView.Frame = new CGRect((View.Frame.Width - loadingView.Frame.Width) / 2,
				(View.Frame.Height - loadingView.Frame.Height) / 2,
				loadingView.Frame.Width,
				loadingView.Frame.Height);

			loadingBg.AddSubview(loadingView);
			View.AddSubview(loadingBg);
			loadingView.StartAnimating();

			scannerView = new AVCaptureScannerView(new CGRect(0, 0, View.Frame.Width, View.Frame.Height), Settings, DefaultOverlaySettings, CustomOverlay)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
			};

			View.AddSubview(scannerView);
			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		public Task TorchAsync(bool on)
			=> scannerView?.TorchAsync(on);

		public Task ToggleTorchAsync()
			=> scannerView?.ToggleTorchAsync();

		public bool IsTorchOn
			=> scannerView?.IsTorchOn ?? false;

		public bool IsAnalyzing
		{
			get => scannerView?.IsAnalyzing ?? false;
			set { if (scannerView != null) scannerView.IsAnalyzing = value; }
		}

		public bool HasTorch => scannerView?.HasTorch ?? false;

		public override void ViewDidAppear(bool animated)
		{
			originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

			if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
			{
				UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.Default;
				SetNeedsStatusBarAppearanceUpdate();
			}
			else
				UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);

			Logger.Info("Starting to scan...");

			scannerView.OnBarcodeScanned += OnBarcodeScanned;
		}

		public override void ViewDidDisappear(bool animated)
		{
			scannerView.OnBarcodeScanned -= OnBarcodeScanned;

			if (scannerView != null)
				scannerView.Stop();
		}

		public override void ViewWillDisappear(bool animated)
			=> UIApplication.SharedApplication.SetStatusBarStyle(originalStatusBarStyle, false);

		public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
		{
			if (scannerView != null)
				scannerView.DidRotate(this.InterfaceOrientation);
		}

		public override bool ShouldAutorotate()
		{
			if (Settings.AutoRotate != null && Settings.AutoRotate.HasValue)
				return Settings.AutoRotate.Value;

			return false;
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
			=> UIInterfaceOrientationMask.All;

		public Task AutoFocusAsync()
			=> scannerView?.AutoFocusAsync();

		public Task AutoFocusAsync(int x, int y)
			=> scannerView?.AutoFocusAsync(x, y);

		//void HandleOnScannerSetupComplete()
		//{
		//	BeginInvokeOnMainThread(() =>
		//	{
		//		if (loadingView != null && loadingBg != null && loadingView.IsAnimating)
		//		{
		//			loadingView.StopAnimating();

		//			UIView.BeginAnimations("zoomout");

		//			UIView.SetAnimationDuration(2.0f);
		//			UIView.SetAnimationCurve(UIViewAnimationCurve.EaseOut);

		//			loadingBg.Transform = CGAffineTransform.MakeScale(2.0f, 2.0f);
		//			loadingBg.Alpha = 0.0f;

		//			UIView.CommitAnimations();

		//			loadingBg.RemoveFromSuperview();
		//		}
		//	});
		//}
	}
}

