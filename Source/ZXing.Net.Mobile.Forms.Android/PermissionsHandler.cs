using System;
using System.Collections.Generic;
using ZXing.Mobile;
using System.Linq;
using Android.App;
using System.Threading.Tasks;
using Android.Content.PM;

namespace ZXing.Net.Mobile.Forms.Android
{
    public class PermissionsHandler
    {
        public PermissionsHandler ()
        {
        }

        static TaskCompletionSource<bool> requestCompletion = null;

        public static Task<bool> RequestPermissions (Activity activity)
        {
            if (requestCompletion != null && !requestCompletion.Task.IsCompleted)
                throw new InvalidOperationException ("Already waiting for permission request");


            var permissionsToRequest = new List<string> ();

            // Check and request any permissions
            foreach (var permission in ZxingActivity.RequiredPermissions) {
                if (PlatformChecks.IsPermissionInManifest (activity, permission)) {
                    if (!PlatformChecks.IsPermissionGranted(activity, permission))
                        permissionsToRequest.Add(permission);                        
                }
            }

            if (permissionsToRequest.Any ()) {
                PlatformChecks.RequestPermissions (activity, permissionsToRequest.ToArray (), 101);
                requestCompletion = new TaskCompletionSource<bool> ();

                return requestCompletion.Task;
            }

            return Task.FromResult<bool> (true);
        }

        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCompletion != null && !requestCompletion.Task.IsCompleted) {

                var success = true;

                foreach (var gr in grantResults) {
                    if (gr == Permission.Denied) {
                        success = false;
                        break;
                    }
                }

                requestCompletion.TrySetResult (success);
            }
        }
    }
}

