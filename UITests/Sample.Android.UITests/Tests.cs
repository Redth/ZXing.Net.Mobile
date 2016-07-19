using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;

namespace Sample.Android.UITests
{
    [TestFixture]
    public class Tests
    {
        AndroidApp app;

        [SetUp]
        public void BeforeEachTest ()
        {
            // TODO: If the Android app being tested is included in the solution then open
            // the Unit Tests window, right click Test Apps, select Add App Project
            // and select the app projects that should be tested.
            app = ConfigureApp
                .Android
                // TODO: Update this path to point to your Android app and uncomment the
                // code if the app is not included in the solution.
                //.ApkFile ("../../../Android/bin/Debug/UITestsAndroid.apk")
                .StartApp ();
        }

        //[Test]
        public void Repl ()
        {
            app.Repl ();
        }

        [Test]
        public void DefaultOverlay_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Id ("buttonScanDefaultView"));

            app.WaitForElement (q => q.Class ("ZXingSurfaceView"));
            app.WaitForElement (q => q.Class ("ZxingOverlayView"));

            app.Screenshot ("View Default Scanner");
        }

        [Test]
        public void ContinuousScanning_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Id ("buttonScanContinuous"));

            app.WaitForElement (q => q.Class ("ZXingSurfaceView"));
            app.WaitForElement (q => q.Class ("ZxingOverlayView"));

            app.Screenshot ("View Continuous Scanner");
        }

        [Test]
        public void CustomOverlay_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Id ("buttonScanCustomView"));

            app.WaitForElement (q => q.Class ("ZXingSurfaceView"));
            app.WaitForElement (q => q.Text ("Place a barcode in the camera viewfinder to scan it.  Barcode will scan Automatically."));

            app.Screenshot ("View Custom Overlay Scanner");
        }

        [Test]
        public void FragmentScanner_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Id ("buttonFragment"));

            app.WaitForElement (q => q.Class ("ZXingSurfaceView"));
            app.WaitForElement (q => q.Class ("ZxingOverlayView"));

            app.Screenshot ("View Fragment Scanner");
        }

        [Test]
        public void BarcodeGenerator_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Id ("buttonGenerate"));

            app.WaitForElement (q => q.Class ("ImageView").Id ("imageBarcode"));

            app.Screenshot ("View Barcode Generator");
        }
    }
}

