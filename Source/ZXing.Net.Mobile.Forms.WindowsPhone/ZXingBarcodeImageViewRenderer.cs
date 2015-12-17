using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using Xamarin.Forms.Platform.WindowsPhone;
using System.ComponentModel;
using System.Reflection;
using ZXing.Mobile;
using System.Threading.Tasks;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsPhone
{
    [Preserve(AllMembers = true)]
    public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, Image>
    {
        public static void Init()
        {
        }

        ZXingBarcodeImageView formsView;
        Image imageView;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
        {
            formsView = Element;

            if (imageView == null)
            {

                imageView = new Image();

                base.SetNativeControl(imageView);
            }

            var writer = new ZXing.Mobile.BarcodeWriter();

            if (formsView != null && formsView.BarcodeOptions != null)
                writer.Options = formsView.BarcodeOptions;
            if (formsView != null && formsView.BarcodeFormat != null)
                writer.Format = formsView.BarcodeFormat;

            var value = formsView != null ? formsView.BarcodeValue : string.Empty;

            var image = writer.Write(value);

            imageView.SetImageBitmap(image);

            base.OnElementChanged(e);
        }
    }
}

