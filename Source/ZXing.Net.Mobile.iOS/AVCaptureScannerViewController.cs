using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

using UIKit;
using Foundation;
using AVFoundation;
using CoreGraphics;

using ZXing;

namespace ZXing.Mobile
{	
    public class AVCaptureScannerViewController : UIViewController, IScannerViewController
	{
		AVCaptureScannerView scannerView;

		public event Action<ZXing.Result> OnScannedResult;

		public MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public MobileBarcodeScanner Scanner { get;set; }
        public bool ContinuousScanning { get;set; }

		UIActivityIndicatorView loadingView;
		UIView loadingBg;

		public AVCaptureScannerViewController(MobileBarcodeScanningOptions options, MobileBarcodeScanner scanner)
		{
			this.ScanningOptions = options;
			this.Scanner = scanner;

			var appFrame = UIScreen.MainScreen.ApplicationFrame;

			View.Frame = new CGRect(0, 0, appFrame.Width, appFrame.Height);
			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		public UIViewController AsViewController()
		{
			return this;
		}

		public void Cancel()
		{
			this.InvokeOnMainThread(() => scannerView.StopScanning());
        }

		UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewDidLoad ()
		{
            loadingBg = new UIView (this.View.Frame) { 
                BackgroundColor = UIColor.Black, 
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight 
            };
            loadingView = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.WhiteLarge) {
                AutoresizingMask = UIViewAutoresizing.FlexibleMargins
            };
			loadingView.Frame = new CGRect ((this.View.Frame.Width - loadingView.Frame.Width) / 2, 
				(this.View.Frame.Height - loadingView.Frame.Height) / 2,
				loadingView.Frame.Width, 
				loadingView.Frame.Height);
			
			loadingBg.AddSubview (loadingView);
			View.AddSubview (loadingBg);
			loadingView.StartAnimating ();

			scannerView = new AVCaptureScannerView(new CGRect(0, 0, this.View.Frame.Width, this.View.Frame.Height));
			scannerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			scannerView.UseCustomOverlayView = this.Scanner.UseCustomOverlay;
			scannerView.CustomOverlayView = this.Scanner.CustomOverlay;
			scannerView.TopText = this.Scanner.TopText;
			scannerView.BottomText = this.Scanner.BottomText;
			scannerView.CancelButtonText = this.Scanner.CancelButtonText;
			scannerView.FlashButtonText = this.Scanner.FlashButtonText;
            scannerView.OnCancelButtonPressed += () => {
                Scanner.Cancel ();
            };

			this.View.AddSubview(scannerView);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		public void Torch(bool on)
		{
			if (scannerView != null)
				scannerView.Torch (on);
		}

		public void ToggleTorch()
		{
			if (scannerView != null)
				scannerView.ToggleTorch ();
		}

		public bool IsTorchOn
		{
			get { return scannerView.IsTorchOn; }
		}

        public void PauseAnalysis ()
        {
            scannerView.PauseAnalysis ();
        }

        public void ResumeAnalysis ()
        {
            scannerView.ResumeAnalysis ();
        }

		public override void ViewDidAppear (bool animated)
		{
			originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

			if (UIDevice.CurrentDevice.CheckSystemVersion (7, 0))
			{
				UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.Default;
				SetNeedsStatusBarAppearanceUpdate ();
			}
            else
                UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);

			Console.WriteLine("Starting to scan...");

			scannerView.StartScanning(result => {

                if (!ContinuousScanning) {
				    Console.WriteLine("Stopping scan...");
				    scannerView.StopScanning();
                }

				var evt = this.OnScannedResult;
				if (evt != null)
					evt(result);                
            }, ScanningOptions);
		}

		public override void ViewDidDisappear (bool animated)
		{
			if (scannerView != null)
				scannerView.StopScanning();
		}

		public override void ViewWillDisappear(bool animated)
		{
			UIApplication.SharedApplication.SetStatusBarStyle(originalStatusBarStyle, false);

			//if (scannerView != null)
			//	scannerView.StopScanning();

			//scannerView.RemoveFromSuperview();
			//scannerView.Dispose();			
			//scannerView = null;
		}

		public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation)
		{
			if (scannerView != null)
				scannerView.DidRotate (this.InterfaceOrientation);

			//overlayView.LayoutSubviews();
		}	
		public override bool ShouldAutorotate ()
		{
		    if (ScanningOptions.AutoRotate != null)
		    {
		        return (bool)ScanningOptions.AutoRotate;
		    }
		    return false;
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations ()
		{
			return UIInterfaceOrientationMask.All;
		}

		void HandleOnScannerSetupComplete ()
		{
			BeginInvokeOnMainThread (() =>
			{
				if (loadingView != null && loadingBg != null && loadingView.IsAnimating)
				{
					loadingView.StopAnimating ();

					UIView.BeginAnimations("zoomout");

					UIView.SetAnimationDuration(2.0f);
					UIView.SetAnimationCurve(UIViewAnimationCurve.EaseOut);

					loadingBg.Transform = CGAffineTransform.MakeScale(2.0f, 2.0f);
					loadingBg.Alpha = 0.0f;

					UIView.CommitAnimations();


					loadingBg.RemoveFromSuperview();
				}
			});
		}
	}
}

