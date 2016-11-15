using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using UITests.Shared;

namespace UITests
{
    [TestFixture]
    public class InitializationTests
    {
        AndroidApp app;
        Platform platform = Platform.Android;

        [SetUp]
        public void BeforeEachTest ()
        {
            app = (AndroidApp)AppInitializer.StartApp (
                platform, 
                TestConsts.ApkFile, 
                null);

            app.WakeUpAndroidDevice ();
        }

        [TearDown]
        public void AfterEachTest ()
        {
            app.ScreenshotIfFailed ();
        }

        // [Test]
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

        [Test]
        public void BarcodeGenerator_Initializes ()
        {
            app.TakeScreenshot ("App Launches");

            app.Tap (q => q.Id ("buttonGenerate"));

            app.WaitForElement (q => q.Class ("ImageView").Id ("imageBarcode"));

            app.TakeScreenshot ("View Barcode Generator");
        }
    }
}
