using ZXing.Mobile;
using Android.OS;
using Android.App;
using Android.Widget;
using Android.Content.PM;

namespace Sample.Android
{
    [Activity(Label = "ZXing.Net.Mobile", Theme = "@android:style/Theme.Holo.Light",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden)]
    public class FragmentActivity : global::Android.Support.V4.App.FragmentActivity
    {
        ZXingScannerFragment scanFragment;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.FragmentActivity);
        }

        protected override void OnResume()
        {
            base.OnResume();

            var needsPermissionRequest = ZXing.Net.Mobile.Android.PermissionsHandler.NeedsPermissionRequest(this);

            if (needsPermissionRequest)
                ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync(this);

            if (scanFragment == null)
            {
                scanFragment = new ZXingScannerFragment();

                SupportFragmentManager.BeginTransaction()
                    .Replace(Resource.Id.fragment_container, scanFragment)
                    .Commit();
            }

            if (!needsPermissionRequest)
                Scan();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            Permission[] grantResults)
        {
            ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions,
                grantResults);
        }

        protected override void OnPause()
        {
            scanFragment?.StopScanning();

            base.OnPause();
        }

        private void Scan()
        {
            scanFragment?.StartScanning(result =>
            {
                // Null result means scanning was canceled
                if (string.IsNullOrEmpty(result?.Text))
                {
                    Toast.MakeText(this, "Scanning Canceled", ToastLength.Long).Show();
                    return;
                }

                // Otherwise, proceed with result
                RunOnUiThread(() => Toast.MakeText(this, "Scanned: " + result.Text, ToastLength.Short).Show());
            });
        }
    }
}
