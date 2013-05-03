using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using ZXing.Mobile;
using Android.Graphics;
using ZXing;
using ZXing.Common;
using ZXing.PDF417;
using Android.Graphics.Drawables;
using System.IO;

namespace MonoAppTest
{
    [Activity (Label = "MonoAppTest", MainLauncher = true)]
    public class Activity1 : Activity
    {

        bool first = true;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.myButton);

            // Camera Test
            button.Click += delegate
            {
                Scan();
            };
        }

        protected override void OnPostResume()
        {
            base.OnPostResume();
            if (first)
            {
                Scan();
                first = false;
            } 
        }

        public void Scan()
        {
            var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
            options.PossibleFormats = new List<ZXing.BarcodeFormat>() {
                ZXing.BarcodeFormat.PDF_417
            };
            options.TryHarder = true;
            options.AutoRotate = true;
            options.TryInverted = true;

            
            var scanner = new ZXing.Mobile.MobileBarcodeScanner(this);
            //Tell our scanner to use the default overlay
            scanner.UseCustomOverlay = false;
            scanner.CameraUnsupportedMessage = "This Device's Camera is not supported.";
            
            //We can customize the top and bottom text of the default overlay
            scanner.TopText = "Hold the camera up to the barcode\nAbout 6 inches away";
            scanner.BottomText = "Wait for the barcode to automatically scan!";
            scanner.Scan(options).ContinueWith(t => {   
                if (t.Result != null)
                {
                    System.Console.WriteLine("Scanned Barcode: " + t.Result.Text);
                    Toast.MakeText(this, t.Result.Text, ToastLength.Short);
                }
            });
        }
    }
}


