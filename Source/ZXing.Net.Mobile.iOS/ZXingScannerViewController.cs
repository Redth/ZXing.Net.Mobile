using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

using UIKit;
using Foundation;
using AVFoundation;
using CoreGraphics;

using ZXing;

namespace ZXing.Mobile
{	
    public class ZXingScannerViewController : UIViewController, IScannerViewController
	{
		ZXingScannerView scannerView;

		public event Action<ZXing.Result> OnScannedResult;

		public MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public MobileBarcodeScanner Scanner { get;set; }
        public bool ContinuousScanning { get;set; }

		UIActivityIndicatorView loadingView;
		UIView loadingBg;

		public UIView CustomLoadingView { get; set; }

		public ZXingScannerViewController(MobileBarcodeScanningOptions options, MobileBarcodeScanner scanner)
		{
			this.ScanningOptions = options;
			this.Scanner = scanner;

			var appFrame = UIScreen.MainScreen.ApplicationFrame;

			this.View.Frame = new CGRect(0, 0, appFrame.Width, appFrame.Height);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		public UIViewController AsViewController()
		{
			return this;
		}

		public void Cancel()
		{            
			this.InvokeOnMainThread (scannerView.StopScanning);
		}

		UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewDidLoad ()
		{
			loadingBg = new UIView (this.View.Frame) { BackgroundColor = UIColor.Black, AutoresizingMask = UIViewAutoresizing.FlexibleDimensions };
			loadingView = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.WhiteLarge)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleMargins
			};
			loadingView.Frame = new CGRect ((this.View.Frame.Width - loadingView.Frame.Width) / 2, 
				(this.View.Frame.Height - loadingView.Frame.Height) / 2,
				loadingView.Frame.Width, 
				loadingView.Frame.Height);			

			loadingBg.AddSubview (loadingView);
			View.AddSubview (loadingBg);
			loadingView.StartAnimating ();

			scannerView = new ZXingScannerView(new CGRect(0, 0, View.Frame.Width, View.Frame.Height));
			scannerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			scannerView.UseCustomOverlayView = this.Scanner.UseCustomOverlay;
			scannerView.CustomOverlayView = this.Scanner.CustomOverlay;
			scannerView.TopText = this.Scanner.TopText;
			scannerView.BottomText = this.Scanner.BottomText;
			scannerView.CancelButtonText = this.Scanner.CancelButtonText;
			scannerView.FlashButtonText = this.Scanner.FlashButtonText;
            scannerView.OnCancelButtonPressed += delegate {                
                Scanner.Cancel ();
            };

			//this.View.AddSubview(scannerView);
			this.View.InsertSubviewBelow (scannerView, loadingView);

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

        public void PauseAnalysis ()
        {
            scannerView.PauseAnalysis ();
        }

        public void ResumeAnalysis ()
        {
            scannerView.ResumeAnalysis ();
        }

		public bool IsTorchOn
		{
			get { return scannerView.IsTorchOn; }
		}

		public override void ViewDidAppear (bool animated)
		{
			scannerView.OnScannerSetupComplete += HandleOnScannerSetupComplete;

			originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

			if (UIDevice.CurrentDevice.CheckSystemVersion (7, 0))
			{
				UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.Default;
				SetNeedsStatusBarAppearanceUpdate ();
			}
            else
                UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);

			Console.WriteLine("Starting to scan...");

			Task.Factory.StartNew (() =>
			{
				BeginInvokeOnMainThread(() => scannerView.StartScanning (result => {

                    if (!ContinuousScanning) {
                        Console.WriteLine ("Stopping scan...");
                        scannerView.StopScanning ();
                    }
					
					var evt = this.OnScannedResult;
					if (evt != null)
						evt (result);
                        
                },this.ScanningOptions));
			});
		}

		public override void ViewDidDisappear (bool animated)
		{
			if (scannerView != null)
				scannerView.StopScanning();

			scannerView.OnScannerSetupComplete -= HandleOnScannerSetupComplete;
		}

		public override void ViewWillDisappear(bool animated)
		{
			UIApplication.SharedApplication.SetStatusBarStyle(originalStatusBarStyle, false);
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

		[Obsolete ("Deprecated in iOS6. Replace it with both GetSupportedInterfaceOrientations and PreferredInterfaceOrientationForPresentation")]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			if (ScanningOptions.AutoRotate != null)
            		{
                		return (bool)ScanningOptions.AutoRotate;
            		}
            		return false;
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

