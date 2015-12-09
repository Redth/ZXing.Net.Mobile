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

namespace ZXing.Mobile
{
	class PlatformChecks
	{
		public static bool HasCameraPermission(Context context)
		{
            return HasPermission (context, Android.Manifest.Permission.Camera);
		}

		public static bool HasFlashlightPermission(Context context)
		{
            return HasPermission (context, Android.Manifest.Permission.Flashlight);
		}

		static bool HasPermission(Context context, string permission)
		{
			PermissionInfo pi = null;

			try { pi = context.PackageManager.GetPermissionInfo (permission, PackageInfoFlags.Permissions); }
			catch { }

			return pi != null;
		}
	}
}

