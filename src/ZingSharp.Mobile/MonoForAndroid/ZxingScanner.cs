using System;
using Android.Content;

namespace ZxingSharp.Mobile
{

	public class ZxingScanner : ZxingScannerBase
	{
		public ZxingScanner () : this(Android.App.Application.Context)
		{
		}

		public ZxingScanner (Context context)
		{
			this.Context = context;
		}

		public Context Context { get; private set; }
		public Android.Views.View CustomOverlay { get; set; }
		public int CaptureSound { get;set; }
			
		bool torch = false;

		public override void StartScanning(ZxingScanningOptions options, Action<ZxingBarcodeResult> onFinishedScanning)
		{
			var scanIntent = new Intent(this.Context, typeof(ZxingActivity));

			ZxingActivity.UseCustomView = this.UseCustomOverlay;
			ZxingActivity.CustomOverlayView = this.CustomOverlay;
			ZxingActivity.ScanningOptions = options;
			ZxingActivity.TopText = TopText;
			ZxingActivity.BottomText = BottomText;

			ZxingActivity.OnCanceled += () => 
			{
				onFinishedScanning(null);
			};

			ZxingActivity.OnScanCompleted += (com.google.zxing.Result result) => 
			{
				onFinishedScanning(ZxingBarcodeResult.FromZxingResult(result));
			};

			this.Context.StartActivity(scanIntent);
		}

		public override void StopScanning()
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