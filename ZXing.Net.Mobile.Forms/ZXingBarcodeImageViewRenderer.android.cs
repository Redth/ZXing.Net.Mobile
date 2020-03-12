using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;
using Android.Runtime;
using Xamarin.Forms.Platform.Android;
using System.ComponentModel;
using Android.Widget;
using ZXing.Mobile;

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
			Regenerate();

			base.OnElementPropertyChanged(sender, e);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
		{
			formsView = Element;

			if (imageView == null)
			{
				imageView = new ImageView(Context);

				base.SetNativeControl(imageView);
			}

			Regenerate();

			base.OnElementChanged(e);
		}

		void Regenerate()
		{
			if (formsView != null && !string.IsNullOrEmpty(formsView.BarcodeValue))
			{
				var writer = new ZXing.Mobile.BarcodeWriter();

				if (formsView != null && formsView.BarcodeOptions != null)
					writer.Options = formsView.BarcodeOptions;
				if (formsView != null && formsView.BarcodeFormat != null)
					writer.Format = formsView.BarcodeFormat;

				var value = formsView != null ? formsView.BarcodeValue : string.Empty;

				Device.BeginInvokeOnMainThread(() =>
				{
					var image = writer.Write(value);

					imageView.SetImageBitmap(image);
				});
			}
		}
	}
}

