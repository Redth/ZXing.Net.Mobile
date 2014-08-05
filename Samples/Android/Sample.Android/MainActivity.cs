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
		Button buttonFragmentScanner;

		MobileBarcodeScanner scanner;
	
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			//Create a new instance of our Scanner
			scanner = new MobileBarcodeScanner(this);

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


