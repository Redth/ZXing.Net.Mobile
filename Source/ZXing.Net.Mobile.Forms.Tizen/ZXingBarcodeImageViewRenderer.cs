using ElmSharp;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.Tizen;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Tizen;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Tizen
{
    [Preserve(AllMembers = true)]
    class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, EvasImage>
    {
        private ZXingBarcodeImageView formsView;
        private EvasImage imageView;

        public static void Init()
        {
            var temp = DateTime.Now;
        }
       
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
                imageView = new EvasImage(Xamarin.Forms.Platform.Tizen.Forms.Context.MainWindow);
                base.SetNativeControl(imageView);
            }
            regenerate();
            base.OnElementChanged(e);
        }
        void regenerate()
        {
            if (formsView != null && formsView.BarcodeValue != null)
            {
                var writer = new ZXing.Mobile.BarcodeWriter(Xamarin.Forms.Platform.Tizen.Forms.Context.MainWindow);

                if (formsView != null && formsView.BarcodeOptions != null)
                    writer.Options = formsView.BarcodeOptions;
                if (formsView != null && formsView.BarcodeFormat != null)
                    writer.Format = formsView.BarcodeFormat;

                var value = formsView != null ? formsView.BarcodeValue : string.Empty;

                Device.BeginInvokeOnMainThread(() => {
                    var image = writer.Write(value);
                    imageView.SetSource(image);
                    imageView.IsFilled = true;
                    imageView.Resize(image.Size.Height, image.Size.Width);
                    imageView.Show();
                });
            }
        }
    }
}
