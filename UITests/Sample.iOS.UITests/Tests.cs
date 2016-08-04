using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UITests.Shared;
using Xamarin.UITest;
using Xamarin.UITest.iOS;
using Xamarin.UITest.Queries;

namespace Sample.iOS.UITests
{
    [TestFixture]
    public class Tests
    {
        iOSApp app;

        [SetUp]
        public void BeforeEachTest ()
        {
            var deviceId = Environment.GetEnvironmentVariable ("XTC_DEVICE_ID") ?? "";

            Console.WriteLine ("Using Device: " + deviceId);

            app = ConfigureApp
                .iOS
                .EnableLocalScreenshots ()
                .PreferIdeSettings ()
                .AppBundle ("../../../../Samples/iOS/Sample.iOS/bin/iPhone/Release/ZXingNetMobileiOSSample.app")
                .DeviceIdentifier (deviceId)
                .StartApp ();

            try {
                app.DisplayBarcode ("http://redth.ca/barcodes/blank.png");
            } catch { }
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

            app.Tap (q => q.Marked ("Scan with Default View"));

            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingScannerView"));
            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingDefaultOverlayView"));

            app.TakeScreenshot ("View Default Overlay");
        }

        [Test]
        public void ContinuousScanning_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Marked ("Scan Continuously"));

            app.WaitForElement (q => q.Class ("ZXing_Mobile_AVCaptureScannerView"));
            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingDefaultOverlayView"));

            app.TakeScreenshot ("View Continuous Scanner");
        }

        [Test]
        public void CustomOverlay_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Marked ("Scan with Custom View"));

            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingScannerView"));
            app.WaitForElement (q => q.Text ("Torch"));

            app.TakeScreenshot ("View Custom Overlay Scanner");
        }

        [Test]
        public void AVCaptureEngine_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Marked ("Scan with AVCapture Engine"));

            app.WaitForElement (q => q.Class ("ZXing_Mobile_AVCaptureScannerView"));
            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingDefaultOverlayView"));

            app.TakeScreenshot ("View AVCaptureEngine Scanner");
        }

        //[Test]
        public void BarcodeGenerator_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Marked ("Generate Barcode"));

            app.WaitForElement (q => q.Class ("UIImageView"));

            app.TakeScreenshot ("View Barcode");
        }
    }
}

