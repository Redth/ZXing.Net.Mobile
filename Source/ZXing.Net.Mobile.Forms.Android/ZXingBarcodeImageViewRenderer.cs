using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;
using Android.Runtime;
using Android.App;
using Xamarin.Forms.Platform.Android;
using Android.Views;
using System.ComponentModel;
using System.Reflection;
using Android.Widget;
using ZXing.Mobile;
using System.Threading.Tasks;

[assembly:ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
    [Preserve(AllMembers = true)]
    public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, ImageView>
    {       
        public static void Init ()
        {
        }

        ZXingBarcodeImageView formsView;
        ImageView imageView;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
        {
            formsView = Element;

            if (imageView == null) {

                imageView = new ImageView (Xamarin.Forms.Forms.Context);

                base.SetNativeControl (imageView);     
            }

            var writer = new ZXing.Mobile.BarcodeWriter ();

            if (formsView != null && formsView.BarcodeOptions != null)
                writer.Options = formsView.BarcodeOptions;
            if (formsView != null && formsView.BarcodeFormat != null)
                writer.Format = formsView.BarcodeFormat;

            var value = formsView != null ? formsView.BarcodeValue : string.Empty;

            var image = writer.Write (value);

            imageView.SetImageBitmap (image);

            base.OnElementChanged (e);
        }
    }
}

