using System;
using Xamarin.Forms;
using ZXing.Common;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingBarcodeImageView : Image
    {
        public ZXingBarcodeImageView () : base ()
        {
            
        }

        public static readonly BindableProperty BarcodeFormatProperty =
            BindableProperty.Create<ZXingBarcodeImageView, BarcodeFormat> (
                p => p.BarcodeFormat,
                defaultValue: BarcodeFormat.QR_CODE, 
                defaultBindingMode: BindingMode.TwoWay);
        
        public BarcodeFormat BarcodeFormat {
            get { return (BarcodeFormat)GetValue (BarcodeFormatProperty); }
            set { SetValue (BarcodeFormatProperty, value); }
        }


        public static readonly BindableProperty BarcodeValueProperty =
            BindableProperty.Create<ZXingBarcodeImageView, string> (
                p => p.BarcodeValue, 
                defaultValue: string.Empty, 
                defaultBindingMode: BindingMode.TwoWay);
        
        public string BarcodeValue {
            get { return (string)GetValue (BarcodeValueProperty); }
            set { SetValue (BarcodeValueProperty, value); }
        }


        public static readonly BindableProperty BarcodeOptionsProperty =
            BindableProperty.Create<ZXingBarcodeImageView, EncodingOptions> (
                p => p.BarcodeOptions, 
                defaultValue: new EncodingOptions (), 
                defaultBindingMode: BindingMode.TwoWay);

        public EncodingOptions BarcodeOptions {
            get { return (EncodingOptions)GetValue (BarcodeOptionsProperty); }
            set { SetValue (BarcodeOptionsProperty, value); }
        }
    }
}

