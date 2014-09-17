using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

#if __UNIFIED__
using Foundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

using ZXing;
using ZXing.Mobile;


namespace Sample.iOS
{
	public class HomeViewController : UIViewController
	{
		UIButton buttonCustomScan;
		UIButton buttonDefaultScan;
		UIButton buttonAVCaptureScan;

		public HomeViewController () : base()
		{
			Version sv = new Version (0, 0, 0);
			Version.TryParse (UIDevice.CurrentDevice.SystemVersion, out sv);

			is7orgreater = sv.Major >= 7;
		}

		MobileBarcodeScanner scanner;
		CustomOverlayView customOverlay;
	
		bool is7orgreater = false;

		public override void ViewDidLoad ()
		{
			if (is7orgreater)
				EdgesForExtendedLayout = UIRectEdge.None;
	
			NavigationItem.Title = "ZXing.Net.Mobile";

			//Create a new instance of our scanner
			scanner = new MobileBarcodeScanner(this.NavigationController);

			//Setup our button
			buttonDefaultScan = new UIButton(UIButtonType.RoundedRect);
			buttonDefaultScan.Frame = new RectangleF(20, 80, 280, 40);
			buttonDefaultScan.SetTitle("Scan with Default View", UIControlState.Normal);
			buttonDefaultScan.TouchUpInside += async (sender, e) => 
			{
				//Tell our scanner to use the default overlay
				scanner.UseCustomOverlay = false;
				//We can customize the top and bottom text of the default overlay
				scanner.TopText = "Hold camera up to barcode to scan";
				scanner.BottomText = "Barcode will automatically scan";

				//Start scanning
				var result = await scanner.Scan ();

				HandleScanResult(result);
			};

			buttonCustomScan = new UIButton(UIButtonType.RoundedRect);
			buttonCustomScan.Frame = new RectangleF(20, 20, 280, 40);
			buttonCustomScan.SetTitle("Scan with Custom View", UIControlState.Normal);
			buttonCustomScan.TouchUpInside += async (sender, e) =>
			{
				//Create an instance of our custom overlay
				customOverlay = new CustomOverlayView();
				//Wireup the buttons from our custom overlay
				customOverlay.ButtonTorch.TouchUpInside += delegate {
					scanner.ToggleTorch();		
				};
				customOverlay.ButtonCancel.TouchUpInside += delegate {
					scanner.Cancel();
				};

				//Tell our scanner to use our custom overlay
				scanner.UseCustomOverlay = true;
				scanner.CustomOverlay = customOverlay;

				var result = await scanner.Scan ();
				
				HandleScanResult(result);
			};

			if (is7orgreater)
			{
				buttonAVCaptureScan = new UIButton (UIButtonType.RoundedRect);
				buttonAVCaptureScan.Frame = new RectangleF (20, 140, 280, 40);
				buttonAVCaptureScan.SetTitle ("Scan with AVCapture Engine", UIControlState.Normal);
				buttonAVCaptureScan.TouchUpInside += async (sender, e) =>
				{
					//Tell our scanner to use the default overlay
					scanner.UseCustomOverlay = false;
					//We can customize the top and bottom text of the default overlay
					scanner.TopText = "Hold camera up to barcode to scan";
					scanner.BottomText = "Barcode will automatically scan";

					//Start scanning
					var result = await scanner.Scan (true);

					HandleScanResult (result);
				};
			}

			this.View.AddSubview (buttonDefaultScan);
			this.View.AddSubview (buttonCustomScan);

			if (is7orgreater)
				this.View.AddSubview (buttonAVCaptureScan);
		}



		void HandleScanResult(ZXing.Result result)
		{
			string msg = "";

			if (result != null && !string.IsNullOrEmpty(result.Text))
				msg = "Found Barcode: " + result.Text;
			else
				msg = "Scanning Canceled!";

			this.InvokeOnMainThread(() => {
				var av = new UIAlertView("Barcode Result", msg, null, "OK", null);
				av.Show();
			});
		}
	}
}

