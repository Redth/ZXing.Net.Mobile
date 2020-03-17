using System;
using MonoTouch.Dialog;

using Foundation;
using CoreGraphics;
using UIKit;

using ZXing;
using ZXing.UI;
using System.Collections.Generic;
using System.Linq;

namespace Sample.iOS
{
	public class HomeViewController : DialogViewController
	{
		public HomeViewController() : base(UITableViewStyle.Grouped, new RootElement("ZXing.Net.Mobile"), false)
		{
		}

		CustomOverlayView customOverlay;

		public override void ViewDidLoad()
		{
			Root = new RootElement("ZXing.Net.Mobile") {
				new Section {

					new StyledStringElement ("Scan with Default View", async () => {
						var scanner = new BarcodeScanner(defaultOverlaySettings: new BarcodeScannerDefaultOverlaySettings
						{
							TopText= "Hold camera up to barcode to scan",
							BottomText = "Barcode will automatically scan"
						});

						//Start scanning
						var results = await scanner.ScanOnceAsync();

						HandleScanResult(results);
					}),

					new StyledStringElement ("Scan Continuously", async () => {

						var scanner = new BarcodeScanner(
							new BarcodeScannerSettings {
								DelayBetweenContinuousScans = TimeSpan.FromSeconds(3),
							}, new BarcodeScannerCustomOverlay(customOverlay));

						
						//Start scanning
						await scanner.ScanContinuouslyAsync(HandleScanResult);
					}),

					new StyledStringElement ("Scan with Custom View", async () => {

						//Create an instance of our custom overlay
						customOverlay = new CustomOverlayView();

						var scanner = new BarcodeScanner(
							new BarcodeScannerSettings { AutoRotate = true },
							new BarcodeScannerCustomOverlay(customOverlay));

						//Wireup the buttons from our custom overlay
						customOverlay.ButtonTorch.TouchUpInside += async delegate {
							await scanner.ToggleTorchAsync();
						};
						customOverlay.ButtonCancel.TouchUpInside += async delegate {
							await scanner.CancelAsync();
						};

						var results = await scanner.ScanOnceAsync();

						HandleScanResult(results);
					}),

					new StyledStringElement ("Scan with AVCapture Engine", async () => {
						var scanner = new BarcodeScanner(
							new BarcodeScannerSettings { UseNativeScanning = true },
							new BarcodeScannerDefaultOverlaySettings
							{
								TopText = "Hold camera up to barcode to scan",
								BottomText = "Barcode will automatically scan"
							});
						
						//Start scanning
						var results = await scanner.ScanOnceAsync();

						HandleScanResult (results);
					}),

					new StyledStringElement ("Generate Barcode", () => {
						NavigationController.PushViewController (new ImageViewController (), true);
					})
				}
			};
		}

		void HandleScanResult(ZXing.Result[] results)
		{
			var msg = "";

			if (results != null && results.Any(r => !string.IsNullOrEmpty(r.Text)))
				msg = "Found Barcodes: " + string.Join("; ", results.Select(r => r.Text));
			else
				msg = "Scanning Canceled!";

			this.InvokeOnMainThread(() =>
			{
				var av = new UIAlertView("Barcode Result", msg, null, "OK", null);
				av.Show();
			});
		}

		public void UITestBackdoorScan(string param)
		{
			var expectedFormat = BarcodeFormat.QR_CODE;
			Enum.TryParse(param, out expectedFormat);

			//Create a new instance of our scanner
			var scanner = new BarcodeScanner(
				new BarcodeScannerSettings(
					new ZXing.Common.DecodingOptions
					{
						PossibleFormats = new[] { expectedFormat }
					}));

			Console.WriteLine("Scanning " + expectedFormat);

			//Start scanning
			scanner.ScanOnceAsync().ContinueWith(t =>
			{
				var results = t.Result;

				var result = results.FirstOrDefault();

				var format = result?.BarcodeFormat.ToString() ?? string.Empty;
				var value = result?.Text ?? string.Empty;

				BeginInvokeOnMainThread(() =>
				{
					var av = UIAlertController.Create("Barcode Result", format + "|" + value, UIAlertControllerStyle.Alert);
					av.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Cancel, null));
					PresentViewController(av, true, null);
				});
			});
		}
	}
}
