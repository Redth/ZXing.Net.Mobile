using System;
using System.Threading.Tasks;
using Android.Content;
using ZXing;
using Android.OS;
using Android.Views;

namespace ZXing.UI
{
	public partial class BarcodeScanner
	{
		TaskCompletionSource<Result[]> tcsScan;


		Context GetContext()
			=> Xamarin.Essentials.Platform.CurrentActivity ?? Xamarin.Essentials.Platform.AppContext;

		void PlatformInit()
		{ }

		void PlatformScan(Action<Result[]> scanHandler)
		{
			var ctx = GetContext();
			var scanIntent = new Intent(ctx, typeof(ZXingScannerActivity));

			scanIntent.AddFlags(ActivityFlags.NewTask);

			ZXingScannerActivity.CustomOverlay = CustomOverlay;
			ZXingScannerActivity.Settings = Settings;
			ZXingScannerActivity.DefaultOverlaySettings = DefaultOverlaySettings;

			ZXingScannerActivity.BarcodeScannedHandler = r => scanHandler?.Invoke(r);

			ctx.StartActivity(scanIntent);
		}

		Task PlatformCancelAsync()
			=> ZXingScannerActivity.RequestCancelAsync();

		Task PlatformAutoFocusAsync()
			=> ZXingScannerActivity.RequestAutoFocusAsync();

		Task PlatformTorchAsync(bool on)
			=> ZXingScannerActivity.RequestTorchAsync(on);

		Task PlatformToggleTorchAsync()
			=> ZXingScannerActivity.RequestToggleTorchAsync();

		bool PlatformIsAnalyzing
		{
			get => ZXingScannerActivity.RequestIsAnalyzing;
			set => ZXingScannerActivity.RequestIsAnalyzing = value;
		}

		internal bool PlatformIsTorchOn
			=> ZXingScannerActivity.RequestIsTorchOn;
	}

	public partial class BarcodeScannerCustomOverlay
	{
		public BarcodeScannerCustomOverlay(View nativeView)
			=> NativeView = nativeView;

		public readonly View NativeView;
	}
}
