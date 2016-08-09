using NUnit.Framework;
using Xamarin.UITest;
using UITests.Shared;

namespace UITests
{
    [TestFixture (Platform.Android)]
    [TestFixture (Platform.iOS)]
    public partial class ScanningTests
    {
        IApp app;
        Platform platform;

        public ScanningTests (Platform platform)
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
    }
}

