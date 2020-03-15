using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;

namespace Sample.Forms
{
	public class CustomContinuousScanPage : ContentPage
	{
		ZXingScannerView zxing;
		ZXingDefaultOverlay overlay;

		public CustomContinuousScanPage() : base()
		{
			zxing = new ZXingScannerView
			{
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
				AutomationId = "zxingScannerView",
			};
			zxing.OnBarcodeScanned += (s, e) =>
				Device.BeginInvokeOnMainThread(async () =>
				{
					Console.WriteLine("Found barcode");
					var str = string.Join("; ", e.Results.Select(r => $"{r.Text} | {r.BarcodeFormat}"));

					// Show an alert
					await DisplayAlert("Scanned Barcode(s)", str, "OK");
				});

			overlay = new ZXingDefaultOverlay
			{
				TopText = "Hold your phone up to the barcode",
				BottomText = "Scanning will happen automatically",
				ShowFlashButton = zxing.HasTorch,
				AutomationId = "zxingDefaultOverlay",
			};
			overlay.FlashButtonClicked += (sender, e) =>
			{
				zxing.IsTorchOn = !zxing.IsTorchOn;
			};
			
			var grid = new Grid
			{
				VerticalOptions = LayoutOptions.FillAndExpand,
				HorizontalOptions = LayoutOptions.FillAndExpand,
			};

			var stopButton = new Button
			{
				WidthRequest = 100,
				HeightRequest = 50,
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.End,
				Text = "stop analyzing",
				Command = new Command(() => zxing.IsAnalyzing = false)
			};

			var cancelButton = new Button
			{
				WidthRequest = 100,
				HeightRequest = 50,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.End,
				Text = "cancel",
				Command = new Command(async () => await Navigation.PopAsync())
			};

			var startButton = new Button
			{
				WidthRequest = 100,
				HeightRequest = 50,
				HorizontalOptions = LayoutOptions.End,
				VerticalOptions = LayoutOptions.End,
				Text = "start analyzing",
				Command = new Command(() => zxing.IsAnalyzing = true)
			};
			grid.Children.Add(zxing);
			grid.Children.Add(overlay);
			grid.Children.Add(startButton);
			grid.Children.Add(cancelButton);
			grid.Children.Add(stopButton);

			// The root page of your application
			Content = grid;
		}
	}
}
