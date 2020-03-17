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

namespace ZXing.UI
{
	[Activity(Label = "Scanner", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
	public class ZXingScannerActivity : FragmentActivity, IScannerView
	{
		public static readonly string[] RequiredPermissions = new[] {
			Android.Manifest.Permission.Camera,
			Android.Manifest.Permission.Flashlight
		};

		public static Action<ZXing.Result[]> BarcodeScannedHandler;
		//static Action CanceledHandler;

		static Func<Task> CancelRequestedHandler;
		static Func<bool, Task> TorchRequestedHandler;
		static Func<Task> AutoFocusRequestedHandler;
		static Func<bool> IsTorchOnHandler;
		static Func<bool> IsAnalyzingRequestedGetHandler;
		static Action<bool> IsAnalyzingRequestedSetHandler;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public static Task RequestCancelAsync()
			=> CancelRequestedHandler?.Invoke();

		public static Task RequestTorchAsync(bool torchOn)
			=> TorchRequestedHandler?.Invoke(torchOn);

		public static Task RequestAutoFocusAsync()
			=> AutoFocusRequestedHandler?.Invoke();

		public static bool RequestIsAnalyzing
		{
			get => IsAnalyzingRequestedGetHandler();
			set => IsAnalyzingRequestedSetHandler(value);
		}

		public static Task RequestToggleTorchAsync()
			=> RequestTorchAsync(!RequestIsTorchOn);

		public static bool RequestIsTorchOn
			=> IsTorchOnHandler?.Invoke() ?? false;


		public static BarcodeScannerSettings Settings { get; set; }

		public static BarcodeScannerDefaultOverlaySettings DefaultOverlaySettings { get; set; }

		public static BarcodeScannerCustomOverlay CustomOverlay { get; set; }

		ZXingScannerFragment scannerFragment;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			RequestWindowFeature(WindowFeatures.NoTitle);

			Window.AddFlags(WindowManagerFlags.Fullscreen); //to show
			Window.AddFlags(WindowManagerFlags.KeepScreenOn); //Don't go to sleep while scanning

			if (Settings.AutoRotate.HasValue && !Settings.AutoRotate.Value)
				RequestedOrientation = ScreenOrientation.Nosensor;

			SetContentView(ZXing.Net.Mobile.Resource.Layout.zxingscanneractivitylayout);

			scannerFragment = new ZXingScannerFragment(Settings, DefaultOverlaySettings, CustomOverlay);
            scannerFragment.OnBarcodeScanned += ScannerFragment_OnBarcodeScanned;

			SupportFragmentManager.BeginTransaction()
				.Replace(ZXing.Net.Mobile.Resource.Id.contentFrame, scannerFragment, "ZXINGFRAGMENT")
				.Commit();

			CancelRequestedHandler = CancelScanAsync;
			AutoFocusRequestedHandler = AutoFocusAsync;
			TorchRequestedHandler = TorchAsync;
			IsTorchOnHandler = () => IsTorchOn;
			IsAnalyzingRequestedGetHandler = () => IsAnalyzing;
			IsAnalyzingRequestedSetHandler = a => IsAnalyzing = a;
		}

        void ScannerFragment_OnBarcodeScanned(object sender, BarcodeScannedEventArgs e)
        {
			OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(e.Results));
			BarcodeScannedHandler?.Invoke(e.Results);
        }

        protected override void OnDestroy()
		{
			if (scannerFragment != null)
				scannerFragment.OnBarcodeScanned -= ScannerFragment_OnBarcodeScanned;

			CancelRequestedHandler = null;
			AutoFocusRequestedHandler = null;
			TorchRequestedHandler = null;
			IsTorchOnHandler = null;
			IsAnalyzingRequestedGetHandler = null;
			IsAnalyzingRequestedSetHandler = null;

			Settings = null;
			CustomOverlay = null;

			base.OnDestroy();
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
			=> Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		public Task TorchAsync(bool on)
			=> scannerFragment?.TorchAsync(on);

		public Task ToggleTorchAsync()
			=> scannerFragment?.ToggleTorchAsync();

		public Task AutoFocusAsync()
			=> scannerFragment?.AutoFocusAsync();

		public Task AutoFocusAsync(int x, int y)
			=> scannerFragment?.AutoFocusAsync(x, y);

		public bool HasTorch
			=> scannerFragment?.HasTorch ?? false;

		public bool IsTorchOn
			=> scannerFragment?.IsTorchOn ?? false;

		public bool IsAnalyzing
		{
			get => scannerFragment?.IsAnalyzing ?? false;
			set { if (scannerFragment != null) scannerFragment.IsAnalyzing = value; }
		}

		async Task CancelScanAsync()
		{
			SetResult(Android.App.Result.Ok);
			Finish();
		}

		public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
		{
			switch (keyCode)
			{
				case Keycode.Back:
					CancelScanAsync();
					break;
				case Keycode.Focus:
					return true;
			}

			return base.OnKeyDown(keyCode, e);
		}
	}
}