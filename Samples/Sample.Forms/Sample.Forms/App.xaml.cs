using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace Sample.Forms
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			MainPage = new NavigationPage(new HomePage { Title = "ZXing.Net.Mobile" });
		}

		protected override void OnStart()
		{
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
		}

		public void UITestBackdoorScan(string param)
		{
			var expectedFormat = ZXing.BarcodeFormat.QR_CODE;
			Enum.TryParse(param, out expectedFormat);
			var opts = new ZXing.UI.BarcodeScanningOptions
			{
				PossibleFormats = new List<ZXing.BarcodeFormat> { expectedFormat }
			};

			System.Diagnostics.Debug.WriteLine("Scanning " + expectedFormat);

			var scanPage = new ZXingScannerPage(opts);
			scanPage.OnScanResult += (result) =>
			{
				if (result != null && result.Length > 0 && result[0] != null)
				{
					Device.BeginInvokeOnMainThread(() =>
					{
						var str = result.Select(r => $"{r.Text} | {r.BarcodeFormat}");

						MainPage.Navigation.PopAsync();
						MainPage.DisplayAlert("Barcode Result(s)", string.Join("; ", str), "OK");
					});
				}
			};

			MainPage.Navigation.PushAsync(scanPage);
		}
	}
}
