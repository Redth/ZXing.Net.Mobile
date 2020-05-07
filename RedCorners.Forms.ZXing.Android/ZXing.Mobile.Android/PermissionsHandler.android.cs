using System;
using System.Collections.Generic;
using ZXing.Mobile;
using System.Linq;
using Android.App;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Content;
using Xamarin.Essentials;

namespace ZXing.Net.Mobile.Android
{
	public static class PermissionsHandler
	{
		[Obsolete("Use Xamarin.Essentials.Platform.OnRequestPermissionsResult instead.")]
		public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
			=> Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		public static async Task<bool> RequestPermissionsAsync()
		{
			var camera = await Permissions.RequestAsync<Permissions.Camera>();

			if (camera != PermissionStatus.Granted)
				return false;

			// Only check if it's in the manifest already
			if (Permissions.IsDeclaredInManifest(global::Android.Manifest.Permission.Flashlight))
			{
				var flashlight = await Permissions.RequestAsync<Permissions.Flashlight>();
				if (flashlight != PermissionStatus.Granted)
					return false;
			}

			return true;
		}

		internal static bool IsTorchPermissionDeclared()
			=> Permissions.IsDeclaredInManifest(global::Android.Manifest.Permission.Flashlight);

		[Obsolete("Use Xamarin.Essentials.Permissions instead.")]
		public static bool NeedsPermissionRequest(Activity activity = null)
			=> true;
	}
}
