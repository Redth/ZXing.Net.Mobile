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

		UIView overlayView = null;
		
		public ZXingScannerViewController(MobileBarcodeScanningOptions options, MobileBarcodeScanner scanner)
		{
			this.ScanningOptions = options;
			this.Scanner = scanner;

			var appFrame = UIScreen.MainScreen.ApplicationFrame;

			this.View.Frame = new RectangleF(0, 0, appFrame.Width, appFrame.Height);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		bool torch = false;
		
		public bool IsTorchOn { get { return torch; } }
		
		public void ToggleTorch()
		{
			try
			{
				NSError err;
				
				var device = MonoTouch.AVFoundation.AVCaptureDevice.DefaultDeviceWithMediaType(MonoTouch.AVFoundation.AVMediaType.Video);
				device.LockForConfiguration(out err);
				
				if (!torch)
				{
					device.TorchMode = MonoTouch.AVFoundation.AVCaptureTorchMode.On;
					device.FlashMode = MonoTouch.AVFoundation.AVCaptureFlashMode.On;
				}
				else
				{
					device.TorchMode = MonoTouch.AVFoundation.AVCaptureTorchMode.Off;
					device.FlashMode = MonoTouch.AVFoundation.AVCaptureFlashMode.Off;
				}

				device.UnlockForConfiguration();
				device = null;
				
				torch = !torch;
			}
			catch { }
			
		}
		
		public void Torch(bool on)
		{
			try
			{
				NSError err;
				
				var device = MonoTouch.AVFoundation.AVCaptureDevice.DefaultDeviceWithMediaType(MonoTouch.AVFoundation.AVMediaType.Video);
				device.LockForConfiguration(out err);
				
				if (on)
				{
					device.TorchMode = MonoTouch.AVFoundation.AVCaptureTorchMode.On;
					device.FlashMode = MonoTouch.AVFoundation.AVCaptureFlashMode.On;
				}
				else
				{
					device.TorchMode = MonoTouch.AVFoundation.AVCaptureTorchMode.Off;
					device.FlashMode = MonoTouch.AVFoundation.AVCaptureFlashMode.Off;
				}

				device.UnlockForConfiguration();
				device = null;
				
				torch = on;
			}
			catch { }
			
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
			
			this.View.AddSubview(scannerView);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			
			if (Scanner.UseCustomOverlay && Scanner.CustomOverlay != null)
				overlayView = Scanner.CustomOverlay;
			else
				overlayView = new ZXingDefaultOverlayView(this.Scanner, new RectangleF(0, 0, this.View.Frame.Width, this.View.Frame.Height),
				                                          () => Scanner.Cancel(), () => Scanner.ToggleTorch());
			
			if (overlayView != null)
			{
				UITapGestureRecognizer tapGestureRecognizer = new UITapGestureRecognizer ();

				tapGestureRecognizer.AddTarget (() => {

					var pt = tapGestureRecognizer.LocationInView(overlayView);

					//scannerView.Focus(pt);

					Console.WriteLine("OVERLAY TOUCH: " + pt.X + ", " + pt.Y);

				});
				tapGestureRecognizer.CancelsTouchesInView = false;
				tapGestureRecognizer.NumberOfTapsRequired = 1;
				tapGestureRecognizer.NumberOfTouchesRequired = 1;

				overlayView.AddGestureRecognizer (tapGestureRecognizer);

				overlayView.Frame = new RectangleF(0, 0, this.View.Frame.Width, this.View.Frame.Height);
				overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
				
				this.View.AddSubview(overlayView);
				this.View.BringSubviewToFront(overlayView);
			}
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

			overlayView.LayoutSubviews();
		}		
	}
}

