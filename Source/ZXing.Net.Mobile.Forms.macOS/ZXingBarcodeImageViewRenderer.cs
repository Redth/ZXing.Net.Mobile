using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.macOS;
using System.ComponentModel;
using System.Reflection;
using ZXing.Mobile;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.MacOS;
using Foundation;
using AppKit;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.macOS
{
    [Preserve(AllMembers = true)]
    public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, NSImageView>
    {
        public static void Init()
        {
            var temp = DateTime.Now;
        }

        ZXingBarcodeImageView formsView;
        NSImageView imageView;

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            regenerate();

            base.OnElementPropertyChanged(sender, e);
        }

        protected override async void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
        {
            formsView = Element;

            if (imageView == null)
            {

                imageView = new NSImageView();

                base.SetNativeControl(imageView);
            }

            regenerate();

            base.OnElementChanged(e);
        }

        void regenerate()
        {
            if (formsView != null && formsView.BarcodeValue != null)
            {
                var writer = new ZXing.Mobile.BarcodeWriter();

                if (formsView != null && formsView.BarcodeOptions != null)
                    writer.Options = formsView.BarcodeOptions;
                if (formsView != null && formsView.BarcodeFormat != null)
                    writer.Format = formsView.BarcodeFormat;

                var value = formsView != null ? formsView.BarcodeValue : string.Empty;

                Device.BeginInvokeOnMainThread(() => {
                    var image = writer.Write(value);

                    imageView.Image = image;
                });
            }
        }
    }
}

