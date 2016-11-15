using NUnit.Framework;
using UITests.Shared;
using Xamarin.UITest;
using UITests.Shared;

namespace UITests
{
    [TestFixture (Platform.Android)]
    [TestFixture (Platform.iOS)]
    public class InitializationTests
    {
        IApp app;
        Platform platform;

        public InitializationTests (Platform platform)
        {
            this.platform = platform;
        }

        [SetUp]
        public void BeforeEachTest ()
        {
            app = AppInitializer.StartApp (
                platform,
                TestConsts.ApkFile,
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
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("scanWithDefaultOverlay"));

            app.WaitForElement (q => q.Marked ("zxingScannerView"));
            app.WaitForElement (q => q.Marked ("zxingDefaultOverlay"));

            app.Screenshot ("View Default Overlay");
        }

        [Test]
        public void ContinuousScanning_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("scanContinuously"));

            app.WaitForElement (q => q.Marked ("zxingScannerView"));
            app.WaitForElement (q => q.Marked ("zxingDefaultOverlay"));

            app.Screenshot ("View Continuous Scanner");
        }

        [Test]
        public void CustomOverlay_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("scanWithCustomOverlay"));

            app.WaitForElement (q => q.Marked ("zxingScannerView"));
            app.WaitForElement (q => q.Text ("Toggle Torch"));

            app.Screenshot ("View Custom Overlay Scanner");
        }

        [Test]
        public void CustomPage_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("scanWithCustomPage"));

            app.WaitForElement (q => q.Marked ("zxingScannerView"));
            app.WaitForElement (q => q.Marked ("zxingDefaultOverlay"));

            app.Screenshot ("View Custom Page Scanner");
        }

        [Test]
        public void BarcodeGenerator_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("barcodeGenerator"));

            app.WaitForElement (q => q.Marked ("zxingBarcodeImageView"));

            app.Screenshot ("View Barcode");
        }
    }
}

