using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using ZXing;

namespace UITests.Shared
{
    public static class AppExtensions
    {
        public static void TakeScreenshot (this Xamarin.UITest.IApp app, string title, [CallerMemberName] string methodName = null)
        {
            var file = app.Screenshot (title);

            // Don't rename files if we're running in testcloud
            if (Xamarin.UITest.TestEnvironment.IsTestCloud)
                return;
            
            var method = new StackTrace ().GetFrame (1).GetMethod ();
            string className = method.ReflectedType.Name;
            string namespaceName = method.ReflectedType.Namespace;


            var baseDir = Environment.GetEnvironmentVariable ("UITEST_SCREENSHOT_PATH") ?? AppDomain.CurrentDomain.BaseDirectory;
            var newFile = string.Format ("{0}-{1}.jpg", methodName, title);

            var newSubDir = Path.Combine (app.Device.DeviceIdentifier, namespaceName, className);
            var newDir = Path.Combine (baseDir, newSubDir);

            var fullPath = Path.Combine (newDir, newFile);

            Directory.CreateDirectory (newDir);

            Console.WriteLine ("Moving Screenshot -> " + fullPath);

            try {
                if (File.Exists (fullPath))
                    File.Delete (fullPath);
            } catch { }
            
            file.MoveTo (fullPath);
        }

        public static void WakeUpAndroidDevice (this Xamarin.UITest.IApp app)
        {
            // Test Cloud will handle this for us
            if (Xamarin.UITest.TestEnvironment.IsTestCloud)
                return;

            var adbExe = "adb";
            var ext = IsUnix () ? "" : ".exe";
            var androidHome = Environment.GetEnvironmentVariable ("ANDROID_HOME");
            if (!string.IsNullOrEmpty (androidHome) && Directory.Exists (androidHome))
                adbExe = Path.Combine (androidHome, "platform-tools", "adb" + ext);

            if (!File.Exists (adbExe))
                return;
            
            //get dumpsys for power stats which includes screen on/off info
            string power = RunProcess (adbExe, "-s " + app.Device.DeviceIdentifier + " shell dumpsys power");

            //checks if screen is on/off. Two versions for different android versions.
            if (power.Contains ("mScreenOn=false") || power.Contains ("Display Power: state=OFF")) {
                //Sends keycode for power on
                RunProcess (adbExe, "-s " + app.Device.DeviceIdentifier + " shell input keyevent 26");
                //Sends keycode for menu button. This will unlock stock android lockscreen. 
                //Does nothing if lockscreen is disabled
                RunProcess (adbExe, "-s " + app.Device.DeviceIdentifier + " shell input keyevent 82");
            }
        }

        public static void ScreenshotIfFailed (this Xamarin.UITest.IApp app)
        {
            var status = TestContext.CurrentContext?.Result?.Status ?? TestStatus.Inconclusive;

            if (status == TestStatus.Failed) {
                try {
                    app.TakeScreenshot ("Failure", TestContext.CurrentContext.Test.Name);
                } catch { }
            }
        }

        public static bool IsUnix ()
        {
            var platform = (int)Environment.OSVersion.Platform;
            if (platform == (int)PlatformID.MacOSX)
                return true;
            if (platform == 4 || platform == 6 || platform == 128)
                return true;
            return false;
        }

        public static string RunProcess (string executable, string args)
        {
            Console.WriteLine ("RunProcess -> " + executable + " " + args);

            var p = Process.Start (new ProcessStartInfo {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = executable,
                Arguments = args
            });

            p.WaitForExit (10000);
            return p.StandardOutput.ReadToEnd ();
        }

        public static void DisplayBarcode (this Xamarin.UITest.IApp app, string url)
        {
            var host = Environment.GetEnvironmentVariable ("BARCODE_SERVER_URL") ?? "http://localhost:8158";
            var fullUrl = host + "?url=" + System.Net.WebUtility.UrlEncode (url);

            Console.WriteLine ("DisplayBarcode -> " + fullUrl);

            var webClient = new System.Net.WebClient ();
            webClient.DownloadString (fullUrl);
        }

        public static void DisplayBarcode (this Xamarin.UITest.IApp app, BarcodeFormat format, string value)
        {
            var host = Environment.GetEnvironmentVariable ("BARCODE_SERVER_URL");
            if (string.IsNullOrEmpty (host)) {
                Console.WriteLine ("No Barcode Display Server specified, skipping...");
                return;
            }
            var fullUrl = host + "?format=" + System.Net.WebUtility.UrlEncode (format.ToString ()) + "&value=" + System.Net.WebUtility.UrlEncode (value);

            Console.WriteLine ("DisplayBarcode -> " + fullUrl);

            var webClient = new System.Net.WebClient ();
            webClient.DownloadString (fullUrl);
        }

        public static void InvokeScanner (this Xamarin.UITest.IApp app, BarcodeFormat format, Xamarin.UITest.Platform platform)
        {
            if (platform == Xamarin.UITest.Platform.iOS)
                app.Invoke ("UITestBackdoorScan:", format.ToString ());
            else
                app.Invoke ("UITestBackdoorScan", format.ToString ());
        }

        public static void AssertUITestBackdoorResult (this Xamarin.UITest.IApp app, BarcodeFormat format, string value)
        {
            // First wait for the result
            app.WaitForElement (q => q.Marked ("Barcode Result"), "Barcode not scanned, no result found", TimeSpan.FromSeconds (10));

            app.TakeScreenshot ("Scan Result Found");

            var result = app.Query (q => q.Marked (format + "|" + value));

            Assert.AreEqual (1, result.Count ());

            app.Tap (q => q.Marked ("OK"));
        }
    }
}
