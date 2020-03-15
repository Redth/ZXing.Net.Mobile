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
#if __ANDROID_29__
using AndroidX.Fragment.App;
#else
using Android.Support.V4.App;
#endif

using System.Linq;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
	[Activity(Label = "Scanner", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
	public class ZxingActivity : FragmentActivity
	{
		public static readonly string[] RequiredPermissions = new[] {
			Android.Manifest.Permission.Camera,
			Android.Manifest.Permission.Flashlight
		};

		public static Action<ZXing.Result[]> ScannedHandler;
		public static Action CanceledHandler;

		public static Action CancelRequestedHandler;
		public static Action<bool> TorchRequestedHandler;
		public static Action AutoFocusRequestedHandler;
		public static Action PauseAnalysisHandler;
		public static Action ResumeAnalysisHandler;
		public static Func<bool> IsTorchOnHandler;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public static void RequestCancel()
			=> CancelRequestedHandler?.Invoke();

		public static void RequestTorch(bool torchOn)
			=> TorchRequestedHandler?.Invoke(torchOn);

		public static void RequestAutoFocus()
			=> AutoFocusRequestedHandler?.Invoke();

		public static void RequestPauseAnalysis()
			=> PauseAnalysisHandler?.Invoke();

		public static void RequestResumeAnalysis()
			=> ResumeAnalysisHandler?.Invoke();

		public static void ToggleTorch()
			=> RequestTorch(!IsTorchOn);

		public static bool IsTorchOn
			=> IsTorchOnHandler?.Invoke() ?? false;

		
		public static MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public static ScannerOverlaySettings<Android.Views.View> OverlaySettings { get; set; }

		ZXingScannerFragment scannerFragment;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			RequestWindowFeature(WindowFeatures.NoTitle);

			Window.AddFlags(WindowManagerFlags.Fullscreen); //to show
			Window.AddFlags(WindowManagerFlags.KeepScreenOn); //Don't go to sleep while scanning

			if (ScanningOptions.AutoRotate.HasValue && !ScanningOptions.AutoRotate.Value)
				RequestedOrientation = ScreenOrientation.Nosensor;

			SetContentView(ZXing.Net.Mobile.Resource.Layout.zxingscanneractivitylayout);

			scannerFragment = new ZXingScannerFragment(this, OverlaySettings);
			scannerFragment.OnBarcodeScanned += OnBarcodeScanned;
			
			SupportFragmentManager.BeginTransaction()
				.Replace(ZXing.Net.Mobile.Resource.Id.contentFrame, scannerFragment, "ZXINGFRAGMENT")
				.Commit();

			CancelRequestedHandler = CancelScan;
			AutoFocusRequestedHandler = AutoFocus;
			TorchRequestedHandler = SetTorch;
			PauseAnalysisHandler = scannerFragment.PauseAnalysis;
			ResumeAnalysisHandler = scannerFragment.ResumeAnalysis;
			IsTorchOnHandler = () => scannerFragment?.IsTorchOn ?? false;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			scannerFragment.OnBarcodeScanned -= OnBarcodeScanned;
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
			=> Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		public void SetTorch(bool on)
			=> scannerFragment.Torch(on);

		public void AutoFocus()
			=> scannerFragment.AutoFocus();

		public void CancelScan()
		{
			Finish();
			CanceledHandler?.Invoke();
		}

		public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
		{
			switch (keyCode)
			{
				case Keycode.Back:
					CancelScan();
					break;
				case Keycode.Focus:
					return true;
			}

			return base.OnKeyDown(keyCode, e);
		}
	}
}