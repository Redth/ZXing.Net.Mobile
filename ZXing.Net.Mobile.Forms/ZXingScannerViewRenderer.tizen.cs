using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Tizen;
using ZXing.UI;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Tizen;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]

namespace ZXing.Net.Mobile.Forms.Tizen
{
	class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.UI.ZXingMediaView>
	{
		protected ZXingScannerView formsView;
		protected ZXingMediaView zxingWindow;

		public static void Init()
		{
			// Keep linker from stripping empty method
			var temp = DateTime.Now;
		}

		protected override void Dispose(bool disposing)
		{
			zxingWindow?.Dispose();
			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			if (e.OldElement != null)
			{
				// Unsubscribe from event handlers and cleanup any resources
				if (Control != null)
				{
					Control.OnBarcodeScanned -= Control_OnBarcodeScanned;
					Control.Dispose();
					SetNativeControl(null);
				}
			}

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					var ctrl = new ZXingMediaView(Xamarin.Forms.Forms.NativeParent, e.NewElement?.Settings);
					ctrl.Show();
					SetNativeControl(ctrl);

					if (!e.NewElement.IsAnalyzing)
						Control.IsAnalyzing = false;
					if (Control.IsTorchOn)
						Control.TorchAsync(Control.IsTorchOn);

					Control.OnBarcodeScanned += Control_OnBarcodeScanned;

					e.NewElement.AutoFocusHandler = (x, y) =>
					{
						if (Control != null)
						{
							if (x < 0 && y < 0)
								Control.AutoFocusAsync();
							else
								Control.AutoFocusAsync(x, y);
						}
					};
				}
			}

			base.OnElementChanged(e);
		}

		void Control_OnBarcodeScanned(object sender, BarcodeScannedEventArgs e)
			=> Element?.RaiseOnBarcodeScanned(e.Results);

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (Control != null)
			{
				switch (e.PropertyName)
				{
					case nameof(ZXingScannerView.IsTorchOn):
						Control?.TorchAsync(Control?.IsTorchOn ?? false);
						break;
					case nameof(ZXingScannerView.IsAnalyzing):
						Control.IsAnalyzing = formsView.IsAnalyzing;
						break;
				}
			}
		}
	}
}
