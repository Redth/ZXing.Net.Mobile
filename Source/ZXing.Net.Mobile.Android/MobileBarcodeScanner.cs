using System;
using System.Threading.Tasks;
using Android.Content;
using ZXing;
using Android.OS;

namespace ZXing.Mobile
{

	public class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		public const string TAG = "ZXing.Net.Mobile";

        static ActivityLifecycleContextListener lifecycleListener;

		public static void Initialize (Android.App.Application app)
		{
			var version = Build.VERSION.SdkInt;

            if (version >= BuildVersionCodes.IceCreamSandwich) {
                lifecycleListener = new ActivityLifecycleContextListener ();
                app.RegisterActivityLifecycleCallbacks (lifecycleListener);
            }
		}

		public static void Uninitialize (Android.App.Application app)
		{
			var version = Build.VERSION.SdkInt;

			if (version >= BuildVersionCodes.IceCreamSandwich)
				app.UnregisterActivityLifecycleCallbacks (lifecycleListener);
		}

		public Android.Views.View CustomOverlay { get; set; }
		//public int CaptureSound { get;set; }

		bool torch = false;

		Context GetContext (Context context)
		{
			if (context != null)
				return context;
			
			var version = Build.VERSION.SdkInt;

			if (version >= BuildVersionCodes.IceCreamSandwich)
				return lifecycleListener.Context;
			else
				return Android.App.Application.Context;
		}

		public override void ScanContinuously (MobileBarcodeScanningOptions options, Action<Result> scanHandler)
		{
			ScanContinuously (null, options, scanHandler);
		}

		public void ScanContinuously (Context context, MobileBarcodeScanningOptions options, Action<Result> scanHandler)
		{
			var ctx = GetContext (context);
			var scanIntent = new Intent(ctx, typeof(ZxingActivity));

			scanIntent.AddFlags(ActivityFlags.NewTask);

			ZxingActivity.UseCustomOverlayView = this.UseCustomOverlay;
			ZxingActivity.CustomOverlayView = this.CustomOverlay;
			ZxingActivity.ScanningOptions = options;
			ZxingActivity.ScanContinuously = true;
			ZxingActivity.TopText = TopText;
			ZxingActivity.BottomText = BottomText;

			ZxingActivity.ScanCompletedHandler = (Result result) => 
			{
				if (scanHandler != null)
					scanHandler (result);
			};

			ctx.StartActivity(scanIntent);
		}

		public override Task<Result> Scan (MobileBarcodeScanningOptions options)
		{
			return Scan (null, options);
		}
		public Task<Result> Scan (Context context, MobileBarcodeScanningOptions options)
		{
			var ctx = GetContext (context);

			var task = Task.Factory.StartNew(() => {

				var waitScanResetEvent = new System.Threading.ManualResetEvent(false);

				var scanIntent = new Intent(ctx, typeof(ZxingActivity));

				scanIntent.AddFlags(ActivityFlags.NewTask);

				ZxingActivity.UseCustomOverlayView = this.UseCustomOverlay;
				ZxingActivity.CustomOverlayView = this.CustomOverlay;
				ZxingActivity.ScanningOptions = options;
				ZxingActivity.ScanContinuously = false;
				ZxingActivity.TopText = TopText;
				ZxingActivity.BottomText = BottomText;

				Result scanResult = null;

				ZxingActivity.CanceledHandler = () => 
				{
					waitScanResetEvent.Set();
				};

				ZxingActivity.ScanCompletedHandler = (Result result) => 
				{
					scanResult = result;
					waitScanResetEvent.Set();
				};

				ctx.StartActivity (scanIntent);

				waitScanResetEvent.WaitOne();

				return scanResult;
			});

			return task;
		}

		public override void Cancel()
		{
			ZxingActivity.RequestCancel();
		}

		public override void AutoFocus ()
		{
			ZxingActivity.RequestAutoFocus();
		}

		public override void Torch (bool on)
		{
			torch = on;
			ZxingActivity.RequestTorch(on);
		}

		public override void ToggleTorch ()
		{
			Torch (!torch);
		}

		public override void PauseAnalysis ()
		{
			ZxingActivity.RequestPauseAnalysis ();
		}

		public override void ResumeAnalysis ()
		{
			ZxingActivity.RequestResumeAnalysis ();
		}

		public override bool IsTorchOn {
			get {
				return torch;
			}
		}

        internal static void LogDebug (string format, params object [] args)
        {
            Android.Util.Log.Debug ("ZXING", format, args);
        }
        internal static void LogError (string format, params object [] args)
        {
            Android.Util.Log.Error ("ZXING", format, args);
        }
        internal static void LogInfo (string format, params object [] args)
        {
            Android.Util.Log.Info ("ZXING", format, args);
        }
        internal static void LogWarn (string format, params object [] args)
        {
            Android.Util.Log.Warn ("ZXING", format, args);
        }
	}
}
