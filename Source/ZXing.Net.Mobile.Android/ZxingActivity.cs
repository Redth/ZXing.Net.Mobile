using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Hardware;
using Android.Graphics;

using Android.Content;
using Android.Runtime;
using Android.Widget;

using ZXing;
using Android.Support.V4.App;
using System.Linq;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
    [Activity (Label = "Scanner", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
    public class ZxingActivity : FragmentActivity
    {
        public static readonly string[] RequiredPermissions = new[] {
            Android.Manifest.Permission.Camera,
            Android.Manifest.Permission.Flashlight
        };

        public static Action<ZXing.Result> ScanCompletedHandler;
        public static Action CanceledHandler;

        public static Action CancelRequestedHandler;
        public static Action<bool> TorchRequestedHandler;
        public static Action AutoFocusRequestedHandler;
        public static Action PauseAnalysisHandler;
        public static Action ResumeAnalysisHandler;

        public static void RequestCancel ()
        {
            var h = CancelRequestedHandler;
            if (h != null)
                h ();
        }

        public static void RequestTorch (bool torchOn)
        {
            var h = TorchRequestedHandler;
            if (h != null)
                h (torchOn);
        }

        public static void RequestAutoFocus ()
        {
            var h = AutoFocusRequestedHandler;
            if (h != null)
                h ();
        }

        public static void RequestPauseAnalysis ()
        {
            var h = PauseAnalysisHandler;
            if (h != null)
                h ();
        }

        public static void RequestResumeAnalysis ()
        {
            var h = ResumeAnalysisHandler;
            if (h != null)
                h ();
        }

        public static View CustomOverlayView { get; set; }

        public static bool UseCustomOverlayView { get; set; }

        public static MobileBarcodeScanningOptions ScanningOptions { get; set; }

        public static string TopText { get; set; }

        public static string BottomText { get; set; }

        public static bool ScanContinuously { get; set; }

        ZXingScannerFragment scannerFragment;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            this.RequestWindowFeature (WindowFeatures.NoTitle);

            this.Window.AddFlags (WindowManagerFlags.Fullscreen); //to show
            this.Window.AddFlags (WindowManagerFlags.KeepScreenOn); //Don't go to sleep while scanning

            if (ScanningOptions.AutoRotate.HasValue && !ScanningOptions.AutoRotate.Value)
                RequestedOrientation = ScreenOrientation.Nosensor;

            SetContentView (Resource.Layout.zxingscanneractivitylayout);

            scannerFragment = new ZXingScannerFragment ();
            scannerFragment.CustomOverlayView = CustomOverlayView;
            scannerFragment.UseCustomOverlayView = UseCustomOverlayView;
            scannerFragment.TopText = TopText;
            scannerFragment.BottomText = BottomText;

            SupportFragmentManager.BeginTransaction ()
				.Replace (Resource.Id.contentFrame, scannerFragment, "ZXINGFRAGMENT")
				.Commit ();
            
            CancelRequestedHandler = CancelScan;
            AutoFocusRequestedHandler = AutoFocus;
            TorchRequestedHandler = SetTorch;
            PauseAnalysisHandler = scannerFragment.PauseAnalysis;
            ResumeAnalysisHandler = scannerFragment.ResumeAnalysis;
        }

        protected override void OnResume ()
        {
            base.OnResume ();

            if (ZXing.Net.Mobile.Android.PermissionsHandler.NeedsPermissionRequest(this))
                ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync(this);
            else
                StartScanning ();
        }

        public override void OnRequestPermissionsResult (int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        { 
            ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        void StartScanning ()
        {
            scannerFragment.StartScanning (result => {
                var h = ScanCompletedHandler;
                if (h != null)
                    h (result);

                if (!ZxingActivity.ScanContinuously)
                    this.Finish ();
            }, ScanningOptions);
        }

        public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged (newConfig);

            Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "Configuration Changed");
        }

        public void SetTorch (bool on)
        {
            scannerFragment.Torch (on);
        }

        public void AutoFocus ()
        {
            scannerFragment.AutoFocus ();
        }

        public void CancelScan ()
        {
            Finish ();
            var h = CanceledHandler;
            if (h != null)
                h ();
        }

        public override bool OnKeyDown (Keycode keyCode, KeyEvent e)
        {
            switch (keyCode) {
            case Keycode.Back:
                CancelScan ();
                break;
            case Keycode.Focus:
                return true;
            }

            return base.OnKeyDown (keyCode, e);
        }
    }

}