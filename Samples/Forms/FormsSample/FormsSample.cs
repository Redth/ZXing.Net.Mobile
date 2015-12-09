using System;

using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;

namespace FormsSample
{
    public class App : Application
    {
        ZXingScannerView zxing;
        ZXingDefaultOverlay overlay;

        public App ()
        {
            zxing = new ZXingScannerView {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };
            zxing.OnScanResult += (result) => {
                System.Diagnostics.Debug.WriteLine (result.Text);
                zxing.StopScanning ();
            };

            overlay = new ZXingDefaultOverlay {
                TopText = "Hold your phone up to the barcode",
                BottomText = "Scanning will happen automatically",
                ShowFlashButton = true,
            };
            overlay.FlashButtonClicked += (sender, e) => {
                zxing.ToggleFlash ();
            };
            var grid = new Grid {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };
            grid.Children.Add (zxing);
            grid.Children.Add (overlay);

            // The root page of your application
            MainPage = new ContentPage {
                Content = grid
            };
        }


        protected override void OnStart ()
        {
            // Handle when your app starts
            zxing.StartScanning ();
        }

        protected override void OnSleep ()
        {
            // Handle when your app sleeps
            zxing.StopScanning ();
        }

        protected override void OnResume ()
        {
            // Handle when your app resumes

        }
    }
}

