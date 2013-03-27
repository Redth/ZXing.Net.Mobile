using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

using MonoTouch.UIKit;
using MonoTouch.Foundation;
using ZXing;

namespace ZXing.Mobile
{	
	public class ZXingScannerViewController : UIViewController
	{


		ZXingScannerView scannerView;

		public event Action<ZXing.Result> OnScannedResult;

		public MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public MobileBarcodeScanner Scanner { get;set; }
		
		public ZXingScannerViewController(MobileBarcodeScanningOptions options, MobileBarcodeScanner scanner)
			: base()
		{

			this.View.Frame = new RectangleF(0, 0, View.Frame.Width, View.Frame.Height);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;


			Console.WriteLine("SCREEN W="+ UIScreen.MainScreen.Bounds.Width + ", H=" + UIScreen.MainScreen.Bounds.Height);
			Console.WriteLine("SCREEN W="+ View.Frame.Width + ", H=" + View.Frame.Height);

			this.ScanningOptions = options;
			this.Scanner = scanner;

			scannerView = new ZXingScannerView(this.View.Frame);
			scannerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			
			this.View.AddSubview(scannerView);
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;

			UIView overlayView = null;
			
			if (Scanner.UseCustomOverlay && Scanner.CustomOverlay != null)
				overlayView = Scanner.CustomOverlay;
			else
				overlayView = new ZXingDefaultOverlayView(scanner, this.View.Frame,
				                                          () => Scanner.Cancel(), () => Scanner.ToggleTorch());
					
			if (overlayView != null)
			{
				overlayView.Frame = this.View.Frame;



				overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
				this.View.AddSubview(overlayView);
				this.View.BringSubviewToFront(overlayView);
			}
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

		public override void ViewWillAppear (bool animated)
		{
			
			base.ViewWillAppear (animated);
			
			
			
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
			
			if (scannerView != null)
				scannerView.StopScanning();
			
		}

		
		
		
	}
}

