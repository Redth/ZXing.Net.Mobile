using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;
using Android.Runtime;
using Xamarin.Forms.Platform.Android;
using System.ComponentModel;
using Android.Widget;
using ZXing.Mobile;
using Android.Graphics;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
	[Preserve(AllMembers = true)]
	public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, ImageView>
	{
		public ZXingBarcodeImageViewRenderer(global::Android.Content.Context context) : base(context)
		{ }

		public static void Init()
		{
			var temp = DateTime.Now;
		}

		ZXingBarcodeImageView formsView;
		ImageView imageView;

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
				imageView = new ImageView(Context);
				SetNativeControl(imageView);
			}

			Regenerate();

			base.OnElementChanged(e);
		}

		void Regenerate()
		{
			Bitmap image = null;

			void SetImage(Bitmap img)
			{
				try { imageView?.SetImageBitmap(img); }
				catch { }
			}

			BarcodeWriter writer = null;
			string barcodeValue = null;

			if (formsView != null
				&& !string.IsNullOrWhiteSpace(formsView.BarcodeValue)
				&& formsView.BarcodeFormat != BarcodeFormat.All_1D)
			{
				barcodeValue = formsView.BarcodeValue;
				writer = new BarcodeWriter { Format = formsView.BarcodeFormat };
				if (formsView != null && formsView.BarcodeOptions != null)
					writer.Options = formsView.BarcodeOptions;
			}

			// Update or clear out the image depending if we had enough info
			// to instantiate the barcode writer, otherwise null the image
			Device.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var img = writer?.Write(barcodeValue);
					imageView?.SetImageBitmap(img);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to update image: {ex}");
				}
			});
		}
	}
}
