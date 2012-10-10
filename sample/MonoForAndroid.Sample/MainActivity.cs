using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using ZxingSharp.Mobile;

namespace ZxingSharp.MonoForAndroid.Sample
{
	[Activity (Label = "ZxingSharp", MainLauncher = true, ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.KeyboardHidden)]
	public class Activity1 : Activity
	{
		Button buttonScanCustomView;
		Button buttonScanDefaultView;

		ZxingScanner scanner;
	
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			//Create a new instance of our Scanner
			scanner = new ZxingScanner(this);

			buttonScanDefaultView = this.FindViewById<Button>(Resource.Id.buttonScanDefaultView);
			buttonScanDefaultView.Click += delegate {
				
				//Tell our scanner to use the default overlay
				scanner.UseCustomOverlay = false;
				//We can customize the top and bottom text of the default overlay
				scanner.TopText = "Hold the camera up to the barcode\nAbout 6 inches away";
				scanner.BottomText = "Wait for the barcode to automatically scan!";

				//Start scanning
				scanner.StartScanning((barcode) => {
					//Scanning finished callback
					HandleScanResult(barcode);
				});
			};

			buttonScanCustomView = this.FindViewById<Button>(Resource.Id.buttonScanCustomView);
			buttonScanCustomView.Click += delegate {

				//Tell our scanner we want to use a custom overlay instead of the default
				scanner.UseCustomOverlay = true;

				//Inflate our custom overlay from a resource layout
				var zxingOverlay = LayoutInflater.FromContext(this).Inflate(Resource.Layout.ZxingOverlay, null);

				//Find the button from our resource layout and wire up the click event
				var flashButton = zxingOverlay.FindViewById<Button>(Resource.Id.buttonZxingFlash);
				flashButton.Click += (sender, e) => {
					//Tell our scanner to toggle the torch/flash
					scanner.ToggleTorch();
				};

				//Set our custom overlay
				scanner.CustomOverlay = zxingOverlay;

				//Start scanning!
				scanner.StartScanning((barcode) => {
					//Our scanning finished callback
					HandleScanResult(barcode);
				});

			};
		}

		void HandleScanResult (ZxingBarcodeResult result)
		{
			string msg = "";

			if (result != null && !string.IsNullOrEmpty(result.Value))
				msg = "Found Barcode: " + result.Value;
			else
				msg = "Scanning Canceled!";

			Toast.MakeText(this, msg, ToastLength.Short).Show();
		}
	}
}


