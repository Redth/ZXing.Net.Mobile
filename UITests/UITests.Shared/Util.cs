using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

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

            file.MoveTo (fullPath);
        }

        public static void DisplayBarcode (this Xamarin.UITest.IApp app, string url)
        {
            var host = Environment.GetEnvironmentVariable ("BARCODE_SERVER_URL") ?? "http://localhost:8158";

            var webClient = new System.Net.WebClient ();
            webClient.DownloadString (host + "?url=" + System.Net.WebUtility.UrlEncode (url));
        }

        public static void DisplayBarcode (this Xamarin.UITest.IApp app, string format, string value)
        {
            var host = Environment.GetEnvironmentVariable ("BARCODE_SERVER_URL") ?? "http://localhost:8158";

            var webClient = new System.Net.WebClient ();
            webClient.DownloadString (host + "?format=" + System.Net.WebUtility.UrlEncode (format) + "&value=" + System.Net.WebUtility.UrlEncode (value));
        }
    }
}

