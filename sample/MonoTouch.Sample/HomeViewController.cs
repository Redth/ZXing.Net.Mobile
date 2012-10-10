using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using ZxingSharp.Mobile;


namespace ZxingSharp.MonoTouch.Sample
{
	public class HomeViewController : UIViewController
	{
		UIButton buttonCustomScan;
		UIButton buttonDefaultScan;

		public HomeViewController () : base()
		{
		}

		ZxingScanner scanner;
		CustomOverlayView customOverlay;
	
		public override void ViewDidLoad ()
		{
			//Create a new instance of our scanner
			scanner = new ZxingScanner();

			//Setup our button
			buttonDefaultScan = new UIButton(UIButtonType.RoundedRect);
			buttonDefaultScan.Frame = new RectangleF(20, 80, 280, 40);
			buttonDefaultScan.SetTitle("Scan with Default View", UIControlState.Normal);
			buttonDefaultScan.TouchUpInside += (sender, e) => 
			{
				//Tell our scanner to use the default overlay
				scanner.UseCustomOverlay = false;
				//We can customize the top and bottom text of the default overlay
				scanner.TopText = "Hold camera up to barcode to scan";
				scanner.BottomText = "Barcode will automatically scan";

				//Start scanning
				scanner.StartScanning(ZxingScanningOptions.Default, (result) => 
				{
					//Our scanning finished callback
					HandleScanResult(result);
				});
			};

			buttonCustomScan = new UIButton(UIButtonType.RoundedRect);
			buttonCustomScan.Frame = new RectangleF(20, 20, 280, 40);
			buttonCustomScan.SetTitle("Scan with Custom View", UIControlState.Normal);
			buttonCustomScan.TouchUpInside += (sender, e) =>
			{
				//Create an instance of our custom overlay
				customOverlay = new CustomOverlayView();
				//Wireup the buttons from our custom overlay
				customOverlay.ButtonTorch.TouchUpInside += delegate {
					scanner.ToggleTorch();		
				};
				customOverlay.ButtonCancel.TouchUpInside += delegate {
					scanner.StopScanning();
				};

				//Tell our scanner to use our custom overlay
				scanner.UseCustomOverlay = true;
				scanner.CustomOverlay = customOverlay;

				scanner.StartScanning(ZxingScanningOptions.Default, (result) => 
				{
					//Our scanning finished callback
					HandleScanResult(result);
				});
			};

			this.View.AddSubview(buttonDefaultScan);
			this.View.AddSubview(buttonCustomScan);
		}

		void HandleScanResult(ZxingBarcodeResult result)
		{
			string msg = "";

			if (result != null && !string.IsNullOrEmpty(result.Value))
				msg = "Found Barcode: " + result.Value;
			else
				msg = "Scanning Canceled!";

			var av = new UIAlertView("Barcode Result", msg, null, "OK", null);
			av.Show();

		}
	}
}

