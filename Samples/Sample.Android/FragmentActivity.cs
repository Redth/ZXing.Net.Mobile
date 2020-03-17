using System;
using System.Collections.Generic;
using ZXing.UI;
using Android.OS;

using Android.App;
using Android.Widget;
using Android.Content.PM;

namespace Sample.Android
{
	[Activity(Label = "ZXing.Net.Mobile", Theme = "@style/Theme.AppCompat.Light", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden)]
	public class FragmentActivity : AndroidX.Fragment.App.FragmentActivity
	{
		ZXingScannerFragment scanFragment;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.FragmentActivity);
		}

		protected override void OnResume()
		{
			base.OnResume();


			if (scanFragment == null)
			{
				scanFragment = new ZXingScannerFragment();

				SupportFragmentManager.BeginTransaction()
					.Replace(Resource.Id.fragment_container, scanFragment)
					.Commit();
			}

			Scan();
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
			=> Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);


		void Scan()
		{
			var opts = new BarcodeScannerSettings(new ZXing.Common.DecodingOptions
			{
				PossibleFormats = new List<ZXing.BarcodeFormat> {
					ZXing.BarcodeFormat.QR_CODE
				},
			})
			{
				CameraResolutionSelector = availableResolutions =>
				{

					foreach (var ar in availableResolutions)
					{
						Console.WriteLine("Resolution: " + ar.Width + "x" + ar.Height);
					}
					return null;
				}
			};
		}
	}
}
