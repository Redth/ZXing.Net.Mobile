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
	class PlatformChecks
	{
		public static bool IsPermissionInManifest(Context context, string permission)
		{            
			PermissionInfo pi = null;

			try { pi = context.PackageManager.GetPermissionInfo (permission, PackageInfoFlags.Permissions); }
			catch { }

            if (pi == null)
                return false;
            
            return true;
		}

        public static bool IsPermissionGranted (Context context, string permission)
        {
            return ContextCompat.CheckSelfPermission(context, permission) == Permission.Granted;            
        }

        public static bool RequestPermissions (Activity activity, string[] permissions, int requestCode)
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
    }
}

