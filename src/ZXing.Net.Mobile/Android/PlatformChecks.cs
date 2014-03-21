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
		public const string PERMISSION_CAMERA = "android.permission.CAMERA";
		public const string PERMISSION_FLASHLIGHT = "android.permission.FLASHLIGHT";

		public static bool HasCameraPermission(Context context)
		{
			return HasPermission (context, PERMISSION_CAMERA);
		}

		public static bool HasFlashlightPermission(Context context)
		{
			return HasPermission (context, PERMISSION_FLASHLIGHT);
		}

		static bool HasPermission(Context context, string permission)
		{
			PermissionInfo pi = null;

			try { pi = context.PackageManager.GetPermissionInfo (PERMISSION_CAMERA, PackageInfoFlags.Permissions); }
			catch { }

			return pi != null;
		}
	}
}

