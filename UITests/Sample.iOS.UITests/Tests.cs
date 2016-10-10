using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
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
            // TODO: If the iOS app being tested is included in the solution then open
            // the Unit Tests window, right click Test Apps, select Add App Project
            // and select the app projects that should be tested.
            //
            // The iOS project should have the Xamarin.TestCloud.Agent NuGet package
            // installed. To start the Test Cloud Agent the following code should be
            // added to the FinishedLaunching method of the AppDelegate:
            //
            //    #if ENABLE_TEST_CLOUD
            //    Xamarin.Calabash.Start();
            //    #endif
            app = ConfigureApp
                .iOS
                // TODO: Update this path to point to your iOS app and uncomment the
                // code if the app is not included in the solution.
                //.AppBundle ("../../../iOS/bin/iPhoneSimulator/Debug/Sample.iOS.UITests.iOS.app")
                .StartApp ();
        }

        [Test]
        public void Repl ()
        {
            app.Repl ();
        }

        [Test]
        public void DefaultOverlay_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("Scan with Default View"));

            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingScannerView"));
            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingDefaultOverlayView"));

            app.Screenshot ("View Default Overlay");
        }

        [Test]
        public void ContinuousScanning_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("Scan Continuously"));

            app.WaitForElement (q => q.Class ("ZXing_Mobile_AVCaptureScannerView"));
            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingDefaultOverlayView"));

            app.Screenshot ("View Continuous Scanner");
        }

        [Test]
        public void CustomOverlay_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("Scan with Custom View"));

            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingScannerView"));
            app.WaitForElement (q => q.Text ("Torch"));

            app.Screenshot ("View Custom Overlay Scanner");
        }

        [Test]
        public void AVCaptureEngine_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("Scan with AVCapture Engine"));

            app.WaitForElement (q => q.Class ("ZXing_Mobile_AVCaptureScannerView"));
            app.WaitForElement (q => q.Class ("ZXing_Mobile_ZXingDefaultOverlayView"));

            app.Screenshot ("View AVCaptureEngine Scanner");
        }

        [Test]
        public void BarcodeGenerator_Initializes ()
        {
            app.Screenshot ("App Launches");

            app.Tap (q => q.Marked ("Generate Barcode"));

            app.WaitForElement (q => q.Class ("UIImageView"));

            app.Screenshot ("View Barcode");
        }
    }
}

