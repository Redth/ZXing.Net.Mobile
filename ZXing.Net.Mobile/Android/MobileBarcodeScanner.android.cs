using System;
using System.Threading.Tasks;
using Android.Content;
using ZXing;
using Android.OS;
using Android.Views;

namespace ZXing.Mobile
{

	public partial class MobileBarcodeScanner
	{
		[Obsolete("Use Xamarin.Essentials.Platform.Init instead")]
		public static void Initialize(Android.App.Application app)
			=> Xamarin.Essentials.Platform.Init(app);

		TaskCompletionSource<Result[]> tcsScan;

		Context GetContext()
			=> Xamarin.Essentials.Platform.CurrentActivity ?? Xamarin.Essentials.Platform.AppContext;

		void PlatformInit()
		{ }

		void PlatformScan(Action<Result[]> scanHandler)
		{
			var ctx = GetContext();
			var scanIntent = new Intent(ctx, typeof(ZxingActivity));

			scanIntent.AddFlags(ActivityFlags.NewTask);

			ZxingActivity.OverlaySettings = OverlaySettings.WithView<View>();
			ZxingActivity.ScannedHandler = (Result[] result)
				=> scanHandler?.Invoke(result);

			ctx.StartActivity(scanIntent);
		}

		internal void PlatformCancel()
		{
			ZxingActivity.RequestCancel();
			tcsScan.TrySetResult(null);
		}

		internal void PlatformAutoFocus()
			=> ZxingActivity.RequestAutoFocus();

		internal void PlatformTorch(bool on)
			=> ZxingActivity.RequestTorch(on);

		internal void PlatformToggleTorch()
			=> ZxingActivity.ToggleTorch();

		internal void PlatformPauseAnalysis()
			=> ZxingActivity.RequestPauseAnalysis();

		internal void PlatformResumeAnalysis()
			=> ZxingActivity.RequestResumeAnalysis();

		internal bool PlatformIsTorchOn
			=> ZxingActivity.IsTorchOn;
	}
}
