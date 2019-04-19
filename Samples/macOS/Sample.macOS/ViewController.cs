using System;

using AppKit;
using CoreGraphics;
using Foundation;
using ZXing.Mobile;

namespace Sample.macOS
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        NSImageView imageBarcode;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            imageBarcode = new NSImageView(new CGRect(20, 80, View.Frame.Width - 40, View.Frame.Height - 120));

            View.AddSubview(imageBarcode);

            var barcodeWriter = new BarcodeWriter
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 300,
                    Height = 300,
                    Margin = 30
                }
            };

            var barcode = barcodeWriter.Write("ZXing.Net.Mobile");

            imageBarcode.Image = barcode;
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }
    }
}
