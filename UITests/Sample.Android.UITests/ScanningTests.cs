using System;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using UITests.Shared;

namespace UITests
{
    [TestFixture]
    public partial class ScanningTests
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
    }
}
