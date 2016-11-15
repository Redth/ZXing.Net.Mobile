using NUnit.Framework;
using UITests.Shared;
using Xamarin.UITest;
using Xamarin.UITest.iOS;
using UITests.Shared;

namespace UITests
{
    [TestFixture]
    public class Tests
    {
        iOSApp app;
        Platform platform = Platform.iOS;

        [SetUp]
        public void BeforeEachTest ()
        {
            app = (iOSApp) AppInitializer.StartApp (
                platform, 
                null, 
                TestConsts.iOSBundleId);
        }

        [TearDown]
        public void AfterEachTest ()
        {
            app.ScreenshotIfFailed ();
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

        [Test]
        public void BarcodeGenerator_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Marked ("Generate Barcode"));

            app.WaitForElement (q => q.Class ("UIImageView"));

            app.TakeScreenshot ("View Barcode");
        }
    }
}

