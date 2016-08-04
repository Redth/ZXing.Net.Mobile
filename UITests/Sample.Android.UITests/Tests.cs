using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;
using UITests.Shared;

namespace Sample.Android.UITests
{
    [TestFixture]
    public class InitializationTests
    {
        AndroidApp app;

        [SetUp]
        public void BeforeEachTest ()
        {
            var deviceId = Environment.GetEnvironmentVariable ("XTC_DEVICE_ID") ?? "";

            Console.WriteLine ("Using Device: " + deviceId);

            // TODO: If the Android app being tested is included in the solution then open
            // the Unit Tests window, right click Test Apps, select Add App Project
            // and select the app projects that should be tested.
            app = ConfigureApp
                .Android
                .EnableLocalScreenshots ()
                .PreferIdeSettings ()
                .DeviceSerial (deviceId)
                .ApkFile ("../../../../Samples/Android/Sample.Android/bin/Release/com.altusapps.zxingnetmobile.apk")
                .StartApp ();

            try {
                app.DisplayBarcode ("http://redth.ca/barcodes/blank.png");
            } catch { }
            
            app.WakeUpAndroidDevice ();
        }

        [TearDown]
        public void AfterEachTest ()
        {
            var status = TestContext.CurrentContext?.Result?.Status ?? TestStatus.Inconclusive;

            if (status == TestStatus.Failed) {
                try {
                    app.TakeScreenshot ("Failure", TestContext.CurrentContext.Test.Name);
                } catch { }
            }
        }

        //[Test]
        public void Repl ()
        {
            app.Repl ();
        }

        [Test]
        public void DefaultOverlay_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Id ("buttonScanDefaultView"));

            app.WaitForElement (q => q.Class ("ZXingSurfaceView"));
            app.WaitForElement (q => q.Class ("ZxingOverlayView"));

            app.TakeScreenshot ("View Default Scanner");
        }

        [Test]
        public void ContinuousScanning_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Id ("buttonScanContinuous"));

            app.WaitForElement (q => q.Class ("ZXingSurfaceView"));
            app.WaitForElement (q => q.Class ("ZxingOverlayView"));

            app.TakeScreenshot ("View Continuous Scanner");
        }

        [Test]
        public void CustomOverlay_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Id ("buttonScanCustomView"));

            app.WaitForElement (q => q.Class ("ZXingSurfaceView"));
            app.WaitForElement (q => q.Text ("Place a barcode in the camera viewfinder to scan it.  Barcode will scan Automatically."));

            app.TakeScreenshot ("View Custom Overlay Scanner");
        }

        [Test]
        public void FragmentScanner_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Id ("buttonFragment"));

            app.WaitForElement (q => q.Class ("ZXingSurfaceView"));
            app.WaitForElement (q => q.Class ("ZxingOverlayView"));

            app.TakeScreenshot ("View Fragment Scanner");
        }

        //[Test]
        public void BarcodeGenerator_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Id ("buttonGenerate"));

            app.WaitForElement (q => q.Class ("ImageView").Id ("imageBarcode"));

            app.TakeScreenshot ("View Barcode Generator");
        }
    }
}

