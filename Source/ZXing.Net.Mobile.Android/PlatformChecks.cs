using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using Android.Support.V4.App;
using Android.Support.V4.Content;

namespace ZXing.Mobile
{
    public class PlatformChecks
    {
        public static bool IsPermissionInManifest(Context context, string permission)
        {
            try
            {
                var info = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Permissions);
                return info.RequestedPermissions.Contains(permission);
            }
            catch
            {
            }

            return false;
        }

        public static bool IsPermissionGranted(Context context, string permission)
        {
            return ContextCompat.CheckSelfPermission(context, permission) == Permission.Granted;
        }

        public static bool RequestPermissions(Activity activity, string[] permissions, int requestCode)
        {
            var permissionsToRequest = new List<string>();
            foreach (var permission in permissions)
            {
                if (ContextCompat.CheckSelfPermission(activity, permission) != Permission.Granted)
                    permissionsToRequest.Add(permission);
            }

            if (permissionsToRequest.Any())
            {
                ActivityCompat.RequestPermissions(activity, permissionsToRequest.ToArray(), requestCode);
                return true;
            }

            return false;
        }

        public static bool CheckCameraPermissions(Context context, bool throwOnError = true)
        {
            return CheckPermissions(context, Android.Manifest.Permission.Camera, throwOnError);
        }

        public static bool CheckTorchPermissions(Context context, bool throwOnError = true)
        {
            return CheckPermissions(context, Android.Manifest.Permission.Flashlight, throwOnError);
        }

        public static bool CheckPermissions(Context context, string permission, bool throwOnError = true)
        {
            var result = true;
            var perf = PerformanceCounter.Start();

            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Checking " + permission + "...");

            if (!IsPermissionInManifest(context, permission) || !IsPermissionGranted(context, permission))
            {
                result = false;

                if (throwOnError)
                {
                    var msg = "ZXing.Net.Mobile requires: " + permission + ", but was not found in your AndroidManifest.xml file.";
                    Android.Util.Log.Error("ZXing.Net.Mobile", msg);

                    throw new UnauthorizedAccessException(msg);
                }
            }

            PerformanceCounter.Stop(perf, "CheckPermissions took {0}ms");

            return result;
        }
    }
}

