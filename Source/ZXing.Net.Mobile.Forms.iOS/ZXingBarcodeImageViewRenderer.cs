using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;
using System.ComponentModel;
using System.Reflection;
using ZXing.Mobile;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.iOS;
using Foundation;
using UIKit;

[assembly:ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.iOS
{
    [Preserve(AllMembers = true)]
    public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, UIImageView>
    {       
        public static void Init ()
        {
        }

        ZXingBarcodeImageView formsView;
        UIImageView imageView;

        protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            formsView = Element;

            if (imageView == null) {

                imageView = new UIImageView ();

                base.SetNativeControl (imageView);     
            }

            var writer = new ZXing.Mobile.BarcodeWriter ();

            if (formsView != null && formsView.Options != null)
                writer.Options = formsView.Options;
            if (formsView != null && formsView.Format != null)
                writer.Format = formsView.Format;

            var value = formsView != null ? formsView.BarcodeValue : string.Empty;

            var image = writer.Write (value);

            imageView.Image = image;

            base.OnElementChanged (e);
        }
    }
}

