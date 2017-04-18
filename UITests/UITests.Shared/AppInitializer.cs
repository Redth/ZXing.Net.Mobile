using System;
using System.IO;
using System.Linq;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace UITests
{
    public class AppInitializer
    {
        public static IApp StartApp (Platform platform, string apkFile, string iosBundleId)
        {
            var deviceId = Environment.GetEnvironmentVariable ("XTC_DEVICE_ID") ?? "";

            Console.WriteLine ("Using Device: " + deviceId);

            if (platform == Platform.Android) {
                return ConfigureApp
                    .Android
                    .EnableLocalScreenshots ()
                    .PreferIdeSettings ()
                    .DeviceSerial (deviceId)
                    .ApkFile (apkFile)
                    .StartApp ();
            }

            return ConfigureApp
                .iOS
                .EnableLocalScreenshots ()
                .PreferIdeSettings ()
                .DeviceIdentifier (deviceId)
                .InstalledApp (iosBundleId)
                .StartApp ();
        }
    }
}

