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
            BindableProperty.Create( nameof( BarcodeFormat ), typeof( BarcodeFormat ), typeof( ZXingBarcodeImageView ), 
                defaultValue: BarcodeFormat.QR_CODE, 
                defaultBindingMode: BindingMode.TwoWay );
        
        public BarcodeFormat BarcodeFormat {
            get { return (BarcodeFormat)GetValue (BarcodeFormatProperty); }
            set { SetValue (BarcodeFormatProperty, value); }
        }


        public static readonly BindableProperty BarcodeValueProperty =
            BindableProperty.Create( nameof(BarcodeValue), typeof(string), typeof(ZXingBarcodeImageView),
                defaultValue: string.Empty, 
                defaultBindingMode: BindingMode.TwoWay);
        
        public string BarcodeValue {
            get { return (string)GetValue (BarcodeValueProperty); }
            set { SetValue (BarcodeValueProperty, value); }
        }


        public static readonly BindableProperty BarcodeOptionsProperty =
            BindableProperty.Create( nameof(BarcodeOptions), typeof(EncodingOptions), typeof(ZXingBarcodeImageView),
                defaultValue: new EncodingOptions (), 
                defaultBindingMode: BindingMode.TwoWay);

        public EncodingOptions BarcodeOptions {
            get { return (EncodingOptions)GetValue (BarcodeOptionsProperty); }
            set { SetValue (BarcodeOptionsProperty, value); }
        }
    }
}

