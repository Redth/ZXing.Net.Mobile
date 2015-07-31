using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;
using ZXing;
using ZXing.Mobile;

namespace Sample.Android
{
	[Activity (Label = "ZXing.Net.Mobile", MainLauncher = true, Theme="@android:style/Theme.Holo.Light", ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.KeyboardHidden)]
	public class Activity1 : Activity
	{
		Button buttonScanCustomView;
		Button buttonScanDefaultView;
        Button buttonContinuousScan;
		Button buttonFragmentScanner;
        Button buttonGenerate;

		MobileBarcodeScanner scanner;
	
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

            // Initialize the scanner first so we can track the current context
            MobileBarcodeScanner.Initialize (Application);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			//Create a new instance of our Scanner
			scanner = new MobileBarcodeScanner();

			buttonScanDefaultView = this.FindViewById<Button>(Resource.Id.buttonScanDefaultView);
			buttonScanDefaultView.Click += async delegate {
				
				//Tell our scanner to use the default overlay
				scanner.UseCustomOverlay = false;

				//We can customize the top and bottom text of the default overlay
				scanner.TopText = "Hold the camera up to the barcode\nAbout 6 inches away";
				scanner.BottomText = "Wait for the barcode to automatically scan!";

				//Start scanning
				var result = await scanner.Scan();

				HandleScanResult(result);
			};

            buttonContinuousScan = FindViewById<Button> (Resource.Id.buttonScanContinuous);
            buttonContinuousScan.Click += delegate {

                scanner.UseCustomOverlay = false;

                //We can customize the top and bottom text of the default overlay
                scanner.TopText = "Hold the camera up to the barcode\nAbout 6 inches away";
                scanner.BottomText = "Wait for the barcode to automatically scan!";

                var opt = new MobileBarcodeScanningOptions ();
                opt.DelayBetweenContinuousScans = 3000;

                //Start scanning
                scanner.ScanContinuously(opt, HandleScanResult);
            };

			Button flashButton;
			View zxingOverlay;

			buttonScanCustomView = this.FindViewById<Button>(Resource.Id.buttonScanCustomView);
			buttonScanCustomView.Click += async delegate {

				//Tell our scanner we want to use a custom overlay instead of the default
				scanner.UseCustomOverlay = true;

				//Inflate our custom overlay from a resource layout
				zxingOverlay = LayoutInflater.FromContext(this).Inflate(Resource.Layout.ZxingOverlay, null);

				//Find the button from our resource layout and wire up the click event
				flashButton = zxingOverlay.FindViewById<Button>(Resource.Id.buttonZxingFlash);
				flashButton.Click += (sender, e) => scanner.ToggleTorch();

				//Set our custom overlay
				scanner.CustomOverlay = zxingOverlay;

				//Start scanning!
				var result = await scanner.Scan();

				HandleScanResult(result);
			};

			buttonFragmentScanner = FindViewById<Button> (Resource.Id.buttonFragment);
			buttonFragmentScanner.Click += delegate {
				StartActivity (typeof (FragmentActivity));	
			};

            buttonGenerate = FindViewById<Button> (Resource.Id.buttonGenerate);
            buttonGenerate.Click += delegate {
                StartActivity (typeof (ImageActivity));
            };
		}

		void HandleScanResult (ZXing.Result result)
		{
			string msg = "";

			if (result != null && !string.IsNullOrEmpty(result.Text))
				msg = "Found Barcode: " + result.Text;
			else
				msg = "Scanning Canceled!";

			this.RunOnUiThread(() => Toast.MakeText(this, msg, ToastLength.Short).Show());
		}
	}
}


