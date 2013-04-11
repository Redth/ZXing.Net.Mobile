using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

using MonoTouch.UIKit;
using MonoTouch.Foundation;
using ZXing;
using ZXing.Mobile;
using MonoTouch.AVFoundation;

namespace ZXing.UIImageTool
{	
	public class TestScannerViewController : UIViewController
	{
		TestScannerView scannerView;

		public event Action<ZXing.Result> OnScannedResult;

		public MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public MobileBarcodeScanner Scanner { get;set; }

		UIView overlayView = null;
		
		public TestScannerViewController(MobileBarcodeScanningOptions options, MobileBarcodeScanner scanner)
			: base()
		{

			this.View.Frame = new RectangleF(0, 0, View.Frame.Width, View.Frame.Height);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
		

			Console.WriteLine("SCREEN W="+ UIScreen.MainScreen.Bounds.Width + ", H=" + UIScreen.MainScreen.Bounds.Height);
			Console.WriteLine("SCREEN W="+ View.Frame.Width + ", H=" + View.Frame.Height);

			
			this.ScanningOptions = options;
			this.Scanner = scanner;

			scannerView = new TestScannerView(this.View.Frame);
			scannerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			
			this.View.AddSubview(scannerView);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;

			//if (Scanner.UseCustomOverlay && Scanner.CustomOverlay != null)
			//	overlayView = Scanner.CustomOverlay;
			//else
			//	overlayView = new ZXingDefaultOverlayView(scanner, this.View.Frame,
				                                          //() => Scanner.Cancel(), () => Scanner.ToggleTorch());
					
			if (overlayView != null)
			{
				overlayView.Frame = this.View.Frame;
				overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

				this.View.AddSubview(overlayView);
				this.View.BringSubviewToFront(overlayView);
			}

			var buttonCapture = new UIBarButtonItem(UIBarButtonSystemItem.Save);
			buttonCapture.Clicked += (sender, e) => {
				Console.WriteLine("CLICKED!!!!");

				scannerView.Capture();
			};
			this.NavigationItem.RightBarButtonItem = buttonCapture;

			var buttonClar = new UIBarButtonItem(UIBarButtonSystemItem.Cancel);
			buttonClar.Clicked += (sender, e) => {
				scannerView.Clear();
			};

		
			this.NavigationItem.LeftBarButtonItem = buttonClar;
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

		UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewWillAppear (bool animated)
		{
			
			base.ViewWillAppear (animated);
		
			originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

			UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);
			
			scannerView.StartScanning(result => 
			{

				//Handle scanning result
				if (scannerView != null)
					scannerView.StopScanning();
				
				var evt = this.OnScannedResult;
				if (evt != null)
					evt(result);


			}, this.ScanningOptions);
			

			
		}

		public override void ViewWillDisappear(bool animated)
		{
			base.ViewWillDisappear(animated);

			UIApplication.SharedApplication.SetStatusBarStyle(originalStatusBarStyle, false);
			
			if (scannerView != null)
				scannerView.StopScanning();
			
		}

		public override void TouchesEnded (NSSet touches, UIEvent evt)
		{
			return;

			if (touches == null || touches.Count <= 0)
				return;

			var touch = touches.AnyObject as UITouch;

			//Get the device
			var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);

			if (device == null || touch == null)
				return;

			//See if it supports focusing on a point
			if (device.FocusPointOfInterestSupported && !device.AdjustingFocus)
			{
				NSError err = null;

				//Lock device to config
				if (device.LockForConfiguration(out err))
				{
					//Focus at the point touched
					device.FocusPointOfInterest = touch.LocationInView(this.View);
					device.FocusMode = AVCaptureFocusMode.ModeContinuousAutoFocus;
					device.UnlockForConfiguration();
				}
			}
		}


		public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation)
		{
			scannerView.ResizePreview(this.InterfaceOrientation);

			//overlayView.LayoutSubviews();
		}
		
		
	}
}

