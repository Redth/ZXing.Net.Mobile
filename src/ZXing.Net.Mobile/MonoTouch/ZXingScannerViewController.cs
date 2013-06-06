using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

using MonoTouch.UIKit;
using MonoTouch.Foundation;
using ZXing;
using MonoTouch.AVFoundation;

namespace ZXing.Mobile
{	
	public class ZXingScannerViewController : UIViewController
	{
		ZXingScannerView scannerView;

		public event Action<ZXing.Result> OnScannedResult;

		public MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public MobileBarcodeScanner Scanner { get;set; }

		//UIView overlayView = null;
		
		public ZXingScannerViewController(MobileBarcodeScanningOptions options, MobileBarcodeScanner scanner)
		{
			this.ScanningOptions = options;
			this.Scanner = scanner;

			var appFrame = UIScreen.MainScreen.ApplicationFrame;

			this.View.Frame = new RectangleF(0, 0, appFrame.Width, appFrame.Height);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}


		public void Cancel()
		{
			this.InvokeOnMainThread(() => scannerView.StopScanning());
		}

		UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewDidLoad ()
		{
			scannerView = new ZXingScannerView(new RectangleF(0, 0, this.View.Frame.Width, this.View.Frame.Height));
			scannerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			scannerView.UseCustomOverlay = this.Scanner.UseCustomOverlay;
			scannerView.CustomOverlay = this.Scanner.CustomOverlay;
			scannerView.TopText = this.Scanner.TopText;
			scannerView.BottomText = this.Scanner.BottomText;
			scannerView.CancelButtonText = this.Scanner.CancelButtonText;
			scannerView.FlashButtonText = this.Scanner.FlashButtonText;

			this.View.AddSubview(scannerView);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
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

		public override void ViewDidAppear (bool animated)
		{
			originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;
			
			UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);

			Console.WriteLine("Starting to scan...");

			scannerView.StartScanning(result => {

				Console.WriteLine("Stopping scan...");

				scannerView.StopScanning();

				var evt = this.OnScannedResult;
				if (evt != null)
					evt(result);
				
				
			}, this.ScanningOptions);
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
			scannerView.ResizePreview(this.InterfaceOrientation);

			//overlayView.LayoutSubviews();
		}		
	}
}

