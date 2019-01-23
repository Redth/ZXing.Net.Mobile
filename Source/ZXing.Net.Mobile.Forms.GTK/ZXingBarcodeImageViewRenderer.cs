using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.GTK;
using ZXing.Net.Mobile.GTK;
using Image = Gtk.Image;

[assembly:ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.GTK
{
    public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, Image>
    {
        public static void Init()
        {
            var temp = DateTime.Now;
        }
        
        ZXingBarcodeImageView formsView;
        Image gtkImage;

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // in GTK there are a way to many properties that are changed compared to other platforms
            if (e.PropertyName == ZXingBarcodeImageView.BarcodeValueProperty.PropertyName ||
                e.PropertyName == ZXingBarcodeImageView.BarcodeFormatProperty.PropertyName ||
                e.PropertyName == ZXingBarcodeImageView.BarcodeOptionsProperty.PropertyName)
            {
                Regenerate();
            }

            base.OnElementPropertyChanged(sender, e);
        }
        
        protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
        {
            formsView = Element;

            if (gtkImage == null)
            {
                gtkImage = new Image();

                base.SetNativeControl(gtkImage);
            }

            Regenerate();
            
            base.OnElementChanged(e);
        }

        void Regenerate ()
        {
            if (formsView != null && formsView.BarcodeValue != null)
            {
                var writer = new BarcodeWriter();

                if (formsView != null && formsView.BarcodeOptions != null)
                    writer.Options = formsView.BarcodeOptions;
                if (formsView != null && formsView.BarcodeFormat != null)
                    writer.Format = formsView.BarcodeFormat;

                var value = formsView != null ? formsView.BarcodeValue : string.Empty;

                Device.BeginInvokeOnMainThread(() =>
                {
                    var pixBuf = writer.Write(value);
                    gtkImage.Pixbuf = pixBuf;
                });
            }
        }
    }
}