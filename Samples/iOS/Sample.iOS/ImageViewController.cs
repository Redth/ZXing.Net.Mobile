using System;
using ZXing.Mobile;

#if __UNIFIED__
using UIKit;
using CoreGraphics;
#else
using MonoTouch.UIKit;
using CGRect = System.Drawing.RectangleF;
#endif

namespace Sample.iOS
{
    public class ImageViewController : UIViewController
    {
        public ImageViewController () : base ()
        {
        }

        UIImageView imageBarcode;

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            NavigationItem.Title = "Generate Barcode";
            View.BackgroundColor = UIColor.White;

            imageBarcode = new UIImageView (new CGRect (20, 80, View.Frame.Width - 40, View.Frame.Height - 120));

            View.AddSubview (imageBarcode);

            var barcodeWriter = new BarcodeWriter {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions {
                    Width = 300,
                    Height = 300,
                    Margin = 30
                }
            };

            var barcode = barcodeWriter.Write ("ZXing.Net.Mobile");

            imageBarcode.Image = barcode;
        }
    }
}

