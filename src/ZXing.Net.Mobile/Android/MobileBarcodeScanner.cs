using System;
using System.Threading.Tasks;
using Android.Content;
using ZXing;

namespace ZXing.Mobile
{

    public class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
        public const string TAG = "ZXing.Net.Mobile";

        static ActivityLifecycleContextListener lifecycleListener = new ActivityLifecycleContextListener ();

        public static void Initialize (Android.App.Application app)
        {
            app.RegisterActivityLifecycleCallbacks (lifecycleListener);
        }

        public static void Uninitialize (Android.App.Application app)
        {
            app.UnregisterActivityLifecycleCallbacks (lifecycleListener);
        }

		public Android.Views.View CustomOverlay { get; set; }
		//public int CaptureSound { get;set; }
			
		bool torch = false;

        public override void ScanContinuously (MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {                
            var scanIntent = new Intent(lifecycleListener.Context, typeof(ZxingActivity));

            scanIntent.AddFlags(ActivityFlags.NewTask);

            ZxingActivity.UseCustomOverlayView = this.UseCustomOverlay;
            ZxingActivity.CustomOverlayView = this.CustomOverlay;
            ZxingActivity.ScanningOptions = options;
            ZxingActivity.ScanContinuously = true;
            ZxingActivity.TopText = TopText;
            ZxingActivity.BottomText = BottomText;

            ZxingActivity.OnCanceled += () => 
            {
                ZxingActivity.RequestCancel ();
            };

            ZxingActivity.OnScanCompleted += (Result result) => 
            {
                if (scanHandler != null)
                    scanHandler (result);
            };

            lifecycleListener.Context.StartActivity(scanIntent);
        }

		public override Task<Result> Scan(MobileBarcodeScanningOptions options)
		{
			var task = Task.Factory.StartNew(() => {
			      
				var waitScanResetEvent = new System.Threading.ManualResetEvent(false);

				var scanIntent = new Intent(lifecycleListener.Context, typeof(ZxingActivity));

				scanIntent.AddFlags(ActivityFlags.NewTask);

				ZxingActivity.UseCustomOverlayView = this.UseCustomOverlay;
				ZxingActivity.CustomOverlayView = this.CustomOverlay;
				ZxingActivity.ScanningOptions = options;
				ZxingActivity.TopText = TopText;
				ZxingActivity.BottomText = BottomText;

				Result scanResult = null;

				ZxingActivity.OnCanceled += () => 
				{
					waitScanResetEvent.Set();
				};

				ZxingActivity.OnScanCompleted += (Result result) => 
				{
					scanResult = result;
					waitScanResetEvent.Set();
				};

				lifecycleListener.Context.StartActivity(scanIntent);

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

		public override bool IsTorchOn {
			get {
				return torch;
			}
		}

	}
	
}
