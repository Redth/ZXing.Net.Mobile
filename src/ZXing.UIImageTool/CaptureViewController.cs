using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using MonoTouch.AVFoundation;
using ZXing.Mobile;
using MonoTouch.CoreVideo;
using MonoTouch.CoreMedia;

namespace ZXing.UIImageTool
{
	public class CaptureViewController : UIViewController
	{

		//CaptureView captureView;
		UIButton bbDone;
		ZXingScannerView scannerView;

		public CaptureViewController ()
		{

		}

		public override void ViewDidLoad ()
		{


		}

		public override void ViewDidAppear (bool animated)
		{
			scannerView = new ZXingScannerView(this.View.Frame);
			this.View.AddSubview(scannerView);

			bbDone = new UIButton(UIButtonType.RoundedRect);
			bbDone.Frame = new RectangleF(0, 0, 100, 38);
			bbDone.SetTitle("DONE", UIControlState.Normal);
			
			bbDone.TouchUpInside += (sender, e) => {
				//captureView.Stop();
				scannerView.StopScanning();
				Done();
			};

			this.View.AddSubview(bbDone);
			scannerView.StartScanning(res => {

			}, new MobileBarcodeScanningOptions());

			//captureView.Start();
		}

		public event Action Done;


	}
}

