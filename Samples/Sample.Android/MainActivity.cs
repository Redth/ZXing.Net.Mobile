using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;
using ZXing;
using ZXing.UI;
using System;
using System.Linq;

namespace Sample.Android
{
	[Activity(Label = "ZXing.Net.Mobile", MainLauncher = true, Theme = "@style/Theme.AppCompat.Light", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden)]
	public class Activity1 : AndroidX.AppCompat.App.AppCompatActivity
	{
		Button buttonScanCustomView;
		Button buttonScanDefaultView;
		Button buttonContinuousScan;
		Button buttonFragmentScanner;
		Button buttonGenerate;


		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			Xamarin.Essentials.Platform.Init(Application);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			//Create a new instance of our Scanner


			buttonScanDefaultView = this.FindViewById<Button>(Resource.Id.buttonScanDefaultView);
			buttonScanDefaultView.Click += async delegate
			{
				var scanner = new BarcodeScanner(new BarcodeScannerSettings
				{
					DecodeMultipleBarcodes = true,
				},
					new BarcodeScannerDefaultOverlaySettings
					{
						TopText = "Hold the camera up to the barcode\nAbout 6 inches away",
						BottomText = "Wait for the barcode to automatically scan!"

					});

				//Start scanning
				var result = await scanner.ScanOnceAsync();

				HandleScanResult(result);
			};

			buttonContinuousScan = FindViewById<Button>(Resource.Id.buttonScanContinuous);
			buttonContinuousScan.Click += async delegate
			{
				var scanner = new BarcodeScanner(
					new BarcodeScannerSettings
					{
						DelayBetweenContinuousScans = TimeSpan.FromSeconds(3)
					},
					new BarcodeScannerDefaultOverlaySettings
					{
						TopText = "Hold the camera up to the barcode\nAbout 6 inches away",
						BottomText = "Wait for the barcode to automatically scan!"
					});

				//Start scanning
				await scanner.ScanContinuouslyAsync(r => HandleScanResult(r));
			};

			Button flashButton;
			View zxingOverlay;

			buttonScanCustomView = this.FindViewById<Button>(Resource.Id.buttonScanCustomView);
			buttonScanCustomView.Click += async delegate
			{
				//Inflate our custom overlay from a resource layout
				zxingOverlay = LayoutInflater.FromContext(this).Inflate(Resource.Layout.ZxingOverlay, null);

				var scanner = new BarcodeScanner(
					new BarcodeScannerSettings
					{
						AutoRotate = true,
						DelayBetweenContinuousScans = TimeSpan.FromSeconds(3)
					},
					new BarcodeScannerCustomOverlay (zxingOverlay));

				//Find the button from our resource layout and wire up the click event
				flashButton = zxingOverlay.FindViewById<Button>(Resource.Id.buttonZxingFlash);
				flashButton.Click += (sender, e) => scanner.ToggleTorchAsync();

				//Start scanning!
				var result = await scanner.ScanOnceAsync();

				HandleScanResult(result);
			};

			buttonFragmentScanner = FindViewById<Button>(Resource.Id.buttonFragment);
			buttonFragmentScanner.Click += delegate
			{
				StartActivity(typeof(FragmentActivity));
			};

			buttonGenerate = FindViewById<Button>(Resource.Id.buttonGenerate);
			buttonGenerate.Click += delegate
			{
				StartActivity(typeof(ImageActivity));
			};
		}

		void HandleScanResult(ZXing.Result[] results)
		{
			var msg = "";

			if (results != null && results.Any(r => !string.IsNullOrEmpty(r.Text)))
				msg = "Found Barcodes: " + string.Join("; ", results.Select(r => r.Text));
			else
				msg = "Scanning Canceled!";

			RunOnUiThread(() => Toast.MakeText(this, msg, ToastLength.Short).Show());
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
		{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		[Java.Interop.Export("UITestBackdoorScan")]
		public Java.Lang.String UITestBackdoorScan(string param)
		{
			var expectedFormat = BarcodeFormat.QR_CODE;
			Enum.TryParse(param, out expectedFormat);

			var barcodeScanner = new BarcodeScanner(
				new BarcodeScannerSettings(new ZXing.Common.DecodingOptions
				{
					PossibleFormats = new[] { expectedFormat }
				}));

			Console.WriteLine("Scanning " + expectedFormat);

			//Start scanning
			barcodeScanner.ScanOnceAsync().ContinueWith(t =>
			{

				var result = t.Result?.FirstOrDefault();

				var format = result?.BarcodeFormat.ToString() ?? string.Empty;
				var value = result?.Text ?? string.Empty;

				RunOnUiThread(() =>
				{

					AlertDialog dialog = null;
					dialog = new AlertDialog.Builder(this)
									.SetTitle("Barcode Result")
									.SetMessage(format + "|" + value)
									.SetNeutralButton("OK", (sender, e) =>
									{
										dialog.Cancel();
									}).Create();
					dialog.Show();
				});
			});

			return new Java.Lang.String();
		}
	}
}


