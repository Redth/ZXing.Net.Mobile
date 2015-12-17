using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using Xamarin.Forms.Platform.WinPhone;
using ZXing.Net.Mobile.Forms.WindowsPhone;
using System.ComponentModel;
using System.Reflection;
using ZXing.Mobile;
using System.Threading.Tasks;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsPhone
{
    public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, System.Windows.Controls.Image>
    {
        public static void Init()
        {
        }

        ZXingBarcodeImageView formsView;
        System.Windows.Controls.Image imageView;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
        {
            formsView = Element;

            if (imageView == null)
            {

                imageView = new System.Windows.Controls.Image();

                base.SetNativeControl(imageView);
            }

            var writer = new ZXing.Mobile.BarcodeWriter();

            if (formsView != null && formsView.BarcodeOptions != null)
                writer.Options = formsView.BarcodeOptions;
            if (formsView != null && formsView.BarcodeFormat != null)
                writer.Format = formsView.BarcodeFormat;

            var value = formsView != null ? formsView.BarcodeValue : string.Empty;

            var image = writer.Write(value);

            imageView.Source = image;

            base.OnElementChanged(e);
        }
    }
}

