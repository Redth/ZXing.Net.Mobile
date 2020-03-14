using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.WindowsUniversal;
using Xamarin.Forms.Platform.UWP;
using System.ComponentModel;
using System.Reflection;
using ZXing.Mobile;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsUniversal
{
	[Preserve]
	public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, Windows.UI.Xaml.Controls.Image>
	{
		public static void Init()
		{
			var tmp = DateTime.UtcNow;
		}

		ZXingBarcodeImageView formsView;
		Windows.UI.Xaml.Controls.Image imageView;

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeValue)
				|| e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeOptions)
				|| e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeFormat))
				Regenerate();

			base.OnElementPropertyChanged(sender, e);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
		{
			formsView = Element;

			if (formsView != null && imageView == null)
			{
				imageView = new Windows.UI.Xaml.Controls.Image();
				SetNativeControl(imageView);
			}

			Regenerate();

			base.OnElementChanged(e);
		}

		void Regenerate()
		{
			BarcodeWriter writer = null;
			string barcodeValue = null;

			if (formsView != null
				&& imageView != null
				&& !string.IsNullOrWhiteSpace(formsView.BarcodeValue)
				&& formsView.BarcodeFormat != BarcodeFormat.All_1D)
			{
				writer = new BarcodeWriter { Format = formsView.BarcodeFormat };
				if (formsView.BarcodeOptions != null)
					writer.Options = formsView.BarcodeOptions;
				barcodeValue = formsView.BarcodeValue;
			}

			// Update or clear out the image depending if we had enough info
			// to instantiate the barcode writer, otherwise null the image
			Device.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var image = writer?.Write(barcodeValue);
					if (imageView != null)
						imageView.Source = image;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to update image: {ex}");
				}
			});
		}
	}
}
