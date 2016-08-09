using System;

using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;

namespace FormsSample
{
    public class App : Application
    {       
        public App ()
        {
            MainPage = new NavigationPage (new HomePage());   
        }        

        protected override void OnStart ()
        {
            // Handle when your app starts            
        }

        protected override void OnSleep ()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume ()
        {
            // Handle when your app resumes
        }

        public void UITestBackdoorScan ()
        {
            var scanPage = new ZXingScannerPage ();
            scanPage.OnScanResult += (result) => {
                scanPage.IsScanning = false;

                Device.BeginInvokeOnMainThread (() => {
                    var format = result?.BarcodeFormat.ToString () ?? string.Empty;
                    var value = result?.Text ?? string.Empty;

                    MainPage.Navigation.PopAsync ();
                    MainPage.DisplayAlert ("Barcode Result", format + "|" + value, "OK");
                });
            };

            MainPage.Navigation.PushAsync (scanPage);
        }
    }
}

