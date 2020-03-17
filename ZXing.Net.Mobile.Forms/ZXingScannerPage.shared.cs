using System;
using Xamarin.Forms;
using ZXing.UI;

namespace ZXing.Net.Mobile.Forms
{
	public class ZXingScannerPage : ContentPage
	{
		readonly ZXingScannerView zxing;
		readonly ZXingDefaultOverlay defaultOverlay = null;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public BarcodeScannerSettings Settings { get; }

		public View CustomOverlay { get; }

		public BarcodeScannerDefaultOverlaySettings DefaultOverlaySettings { get; }


		public ZXingScannerPage(BarcodeScannerSettings settings = null, BarcodeScannerDefaultOverlaySettings defaultOverlaySettings = null, View customOverlay = null)
			: base()
		{
			Settings = settings;
			CustomOverlay = customOverlay;
			DefaultOverlaySettings = defaultOverlaySettings;

			zxing = new ZXingScannerView(settings)
			{
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
				AutomationId = "zxingScannerView"
			};

			zxing.OnBarcodeScanned += (s, e)
				=> OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(e.Results));

			View overlayToAdd;
			if (customOverlay == null)
			{
				defaultOverlay = new ZXingDefaultOverlay { AutomationId = "zxingDefaultOverlay" };

				defaultOverlay.SetBinding(ZXingDefaultOverlay.TopTextProperty, nameof(DefaultOverlaySettings.TopText));
				defaultOverlay.SetBinding(ZXingDefaultOverlay.BottomTextProperty, nameof(DefaultOverlaySettings.BottomText));
				defaultOverlay.SetBinding(ZXingDefaultOverlay.ShowFlashButtonProperty, nameof(DefaultOverlaySettings.ShowFlashButton));

				defaultOverlay.FlashButtonClicked += (sender, e) =>
					zxing.IsTorchOn = !zxing.IsTorchOn;

				defaultOverlay.BindingContext = DefaultOverlaySettings;

				overlayToAdd = defaultOverlay;
			}
			else
			{
				overlayToAdd = customOverlay;
			}

			var grid = new Grid
			{
				VerticalOptions = LayoutOptions.FillAndExpand,
				HorizontalOptions = LayoutOptions.FillAndExpand,
			};

			grid.Children.Add(zxing);
			
			if (overlayToAdd != null)
				grid.Children.Add(overlayToAdd);

			// The root page of your application
			Content = grid;
		}

		public void ToggleTorch()
			=> zxing?.ToggleTorch();

		public void AutoFocus()
			=> zxing?.AutoFocus();

		public void AutoFocus(int x, int y)
			=> zxing?.AutoFocus(x, y);
	}
}
