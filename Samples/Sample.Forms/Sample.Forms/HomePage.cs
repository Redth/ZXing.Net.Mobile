using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;

namespace Sample.Forms
{
	public class HomePage : ContentPage
	{
		ZXingScannerPage scanPage;
		Button buttonScanDefaultOverlay;
		Button buttonScanCustomOverlay;
		Button buttonScanContinuously;
		Button buttonScanContinuousCustomPage;
		Button buttonScanCustomPage;
		Button buttonGenerateBarcode;
		

		public HomePage() : base()
		{
			buttonScanDefaultOverlay = new Button
			{
				Text = "Scan with Default Overlay",
				AutomationId = "scanWithDefaultOverlay",
			};
			buttonScanDefaultOverlay.Clicked += async delegate
			{
				scanPage = new ZXingScannerPage();
				scanPage.OnBarcodeScanned += (s, e) =>
				{
					Device.BeginInvokeOnMainThread(async () =>
					{
						await Navigation.PopAsync();
						var str = string.Join("; ", e.Results.Select(r => $"{r.Text} | {r.BarcodeFormat}"));

						await DisplayAlert("Scanned Barcode", str, "OK");
					});
				};

				await Navigation.PushAsync(scanPage);
			};


			buttonScanCustomOverlay = new Button
			{
				Text = "Scan with Custom Overlay",
				AutomationId = "scanWithCustomOverlay",
			};
			buttonScanCustomOverlay.Clicked += async delegate
			{
				// Create our custom overlay
				var customOverlay = new StackLayout
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand
				};
				var torch = new Button
				{
					Text = "Toggle Torch"
				};
				torch.Clicked += delegate
				{
					scanPage.ToggleTorch();
				};
				customOverlay.Children.Add(torch);

				scanPage = new ZXingScannerPage(new ZXing.UI.BarcodeScannerSettings { AutoRotate = true }, customOverlay: customOverlay);
				scanPage.OnBarcodeScanned += (s, e) =>
				{
					Device.BeginInvokeOnMainThread(async () =>
					{
						await Navigation.PopAsync();
						var str = string.Join("; ", e.Results.Select(r => $"{r.Text} | {r.BarcodeFormat}"));
						await DisplayAlert("Scanned Barcode(s)", str, "OK");
					});
				};
				await Navigation.PushAsync(scanPage);
			};


			buttonScanContinuously = new Button
			{
				Text = "Scan Continuously",
				AutomationId = "scanContinuously",
			};
			buttonScanContinuously.Clicked += async delegate
			{
				scanPage = new ZXingScannerPage(new ZXing.UI.BarcodeScannerSettings { DelayBetweenContinuousScans = TimeSpan.FromSeconds(3) });
				scanPage.OnBarcodeScanned += (s, e) =>
					Device.BeginInvokeOnMainThread(async () =>
					{
						var str = string.Join("; ", e.Results.Select(r => $"{r.Text} | {r.BarcodeFormat}"));

						await DisplayAlert("Scanned Barcode", str, "OK");
					});

				await Navigation.PushAsync(scanPage);
			};

			buttonScanCustomPage = new Button
			{
				Text = "Scan with Custom Page",
				AutomationId = "scanWithCustomPage",
			};
			buttonScanCustomPage.Clicked += async delegate
			{
				var customScanPage = new CustomScanPage();
				await Navigation.PushAsync(customScanPage);
			};

			buttonScanContinuousCustomPage = new Button
			{
				Text = "Scan Continuously with Custom Page",
				AutomationId = "scanContinuouslyWithCustomPage",
			};
			buttonScanContinuousCustomPage.Clicked += async delegate
			{
				var customContinuousScanPage = new CustomContinuousScanPage();
				await Navigation.PushAsync(customContinuousScanPage);
			};


			buttonGenerateBarcode = new Button
			{
				Text = "Barcode Generator",
				AutomationId = "barcodeGenerator",
			};
			buttonGenerateBarcode.Clicked += async delegate
			{
				await Navigation.PushAsync(new BarcodePage());
			};

			var stack = new StackLayout();
			stack.Children.Add(buttonScanDefaultOverlay);
			stack.Children.Add(buttonScanCustomOverlay);
			stack.Children.Add(buttonScanContinuously);
			stack.Children.Add(buttonScanCustomPage);
			stack.Children.Add(buttonScanContinuousCustomPage);
			stack.Children.Add(buttonGenerateBarcode);

			Content = stack;
		}
	}
}
