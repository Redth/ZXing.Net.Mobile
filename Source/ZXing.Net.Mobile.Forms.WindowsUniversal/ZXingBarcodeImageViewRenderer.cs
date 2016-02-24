using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.WindowsUniversal;
using Xamarin.Forms.Platform.UWP;
using System.ComponentModel;
using System.Reflection;
using ZXing.Mobile;
using System.Threading.Tasks;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsUniversal
{
    public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, Windows.UI.Xaml.Controls.Image>
    {
        public static void Init()
        {
        }

        ZXingBarcodeImageView formsView;
        Windows.UI.Xaml.Controls.Image imageView;

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            regenerate();

            base.OnElementPropertyChanged(sender, e);
        }
        protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
        {
            formsView = Element;

            if (imageView == null)
            {

                imageView = new Windows.UI.Xaml.Controls.Image();

                base.SetNativeControl(imageView);
            }

            regenerate();
            
            base.OnElementChanged(e);
        }

        void regenerate ()
        {
            if (formsView != null && formsView.BarcodeValue != null)
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

                    imageView.Source = image;
                });
            }
        }
    }
}

