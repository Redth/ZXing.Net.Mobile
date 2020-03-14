using ElmSharp;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.Tizen;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Tizen;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Tizen
{
	[Preserve(AllMembers = true)]
	class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, EvasImage>
	{
		ZXingBarcodeImageView formsView;
		EvasImage imageView;

		public static void Init()
		{
			var temp = DateTime.Now;
		}

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
				imageView = new EvasImage(Xamarin.Forms.Forms.NativeParent);
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
				barcodeValue = formsView.BarcodeValue;
				writer = new BarcodeWriter(imageView.Parent) { Format = formsView.BarcodeFormat };
				if (formsView.BarcodeOptions != null)
					writer.Options = formsView.BarcodeOptions;
			}

			// Update or clear out the image depending if we had enough info
			// to instantiate the barcode writer, otherwise null the image
			Device.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var image = writer.Write(barcodeValue);
					if (imageView != null)
					{
						imageView.SetSource(image);
						imageView.IsFilled = true;
						imageView.Resize(image.Size.Height, image.Size.Width);
						imageView.Show();
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to update image: {ex}");
				}
			});
		}
	}
}
