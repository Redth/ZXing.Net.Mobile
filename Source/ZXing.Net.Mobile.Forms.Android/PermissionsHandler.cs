using System;
using System.Collections.Generic;
using ZXing.Mobile;
using System.Linq;
using Android.App;
using System.Threading.Tasks;
using Android.Content.PM;

namespace ZXing.Net.Mobile.Forms.Android
{
    public static class PermissionsHandler
    {
        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            // Forward the call to the generic android implementation
            Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

