using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;

namespace BarcodeDisplayServer
{
    [Activity (Label = "Barcode Display Server", 
               ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape, 
               LaunchMode = Android.Content.PM.LaunchMode.SingleTop, 
               MainLauncher = true, 
               Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        ImageView imageViewBarcode;

        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);

            imageViewBarcode = FindViewById<ImageView> (Resource.Id.imageViewBarcode);

            StartService (new Intent (this, typeof (HttpListenerService)));
        }

        protected override void OnResume ()
        {
            base.OnResume ();

            // Full screen
            var opt = Android.Views.SystemUiFlags.LayoutStable
                             | Android.Views.SystemUiFlags.HideNavigation
                             | Android.Views.SystemUiFlags.Fullscreen
                             | Android.Views.SystemUiFlags.LayoutHideNavigation
                             | Android.Views.SystemUiFlags.LayoutFullscreen
                             | Android.Views.SystemUiFlags.ImmersiveSticky;
            Window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)opt;
        }

        protected override void OnNewIntent (Android.Content.Intent intent)
        {
            base.OnNewIntent (intent);

            // Get the barcode options from the intent
            var barcodeFormat = ZXing.BarcodeFormat.QR_CODE;
            if (intent.HasExtra ("FORMAT"))
                System.Enum.TryParse<ZXing.BarcodeFormat> (intent.GetStringExtra ("FORMAT"), out barcodeFormat);

            var barcodeValue = string.Empty;
            if (intent.HasExtra ("VALUE"))
                barcodeValue = intent.GetStringExtra ("VALUE") ?? string.Empty;

            var barcodeUrl = string.Empty;
            if (intent.HasExtra ("URL"))
                barcodeUrl = intent.GetStringExtra ("URL") ?? string.Empty;

            // Can set from a URL or generate from a format/value
            if (!string.IsNullOrEmpty (barcodeUrl)) {
                SetBarcode (barcodeUrl);
            } else if (!string.IsNullOrEmpty (barcodeValue)) {
                SetBarcode (barcodeFormat, barcodeValue);
            }
        }

        void SetBarcode (string url)
        {
            Square.Picasso.Picasso.With (this)
                  .Load (url)
                  .Into (imageViewBarcode);
        }

        void SetBarcode (ZXing.BarcodeFormat format, string value)
        {
            var w = new ZXing.BarcodeWriter ();

            w.Options = new ZXing.Common.EncodingOptions {
                Width = imageViewBarcode.Width,
                Height = imageViewBarcode.Height,
            };
            w.Format = format;

            try {
                using (var bitmap = w.Write (value)) {
                    imageViewBarcode.SetImageBitmap (bitmap);
                }
            } catch {
                imageViewBarcode.SetImageDrawable (null);
            }
        }
    }
}


