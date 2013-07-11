
using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;

namespace ZXing.Mobile
{
	public class ZXingScannerFragment : Fragment
	{
	    public ZXingScannerFragment() 
        {
            ScanningOptions = MobileBarcodeScanningOptions.Default;
            UseCustomView = false;
	    }

		public ZXingScannerFragment(Action<Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
		{
            Callback = scanResultCallback;
			ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;
			UseCustomView = false;
		}

	    public Action<Result> Callback { get; set; }

	    public override View OnCreateView (LayoutInflater layoutInflater, ViewGroup viewGroup, Bundle bundle)
		{
			var frame = (FrameLayout)layoutInflater.Inflate(Resource.Layout.zxingscannerfragmentlayout, null);

			var layoutParams = new LinearLayout.LayoutParams (ViewGroup.LayoutParams.FillParent, ViewGroup.LayoutParams.FillParent);
							
			try
			{
				scanner = new ZXingSurfaceView (this.Activity, ScanningOptions, Callback);
				frame.AddView(scanner, layoutParams);


				if (!UseCustomView)
				{
					zxingOverlay = new ZxingOverlayView (this.Activity);
					zxingOverlay.TopText = TopText ?? "";
					zxingOverlay.BottomText = BottomText ?? "";

					frame.AddView (zxingOverlay, layoutParams);
				}
				else if (CustomOverlayView != null)
				{
					frame.AddView(CustomOverlayView, layoutParams);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine ("Create Surface View Failed: " + ex);
			}
			return frame;
		}

		public override void OnPause ()
		{
			base.OnPause ();

			scanner.ShutdownCamera();
		}

		public View CustomOverlayView { get;set; }
		public bool UseCustomView { get; set; }
		public MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public string TopText { get;set; }
		public string BottomText { get;set; }
		
		ZXingSurfaceView scanner;
		ZxingOverlayView zxingOverlay;

		public void SetTorch(bool on)
		{
			this.scanner.Torch(on);
		}
		
		public void AutoFocus()
		{
			this.scanner.AutoFocus();
		}

		public void Shutdown()
		{
			scanner.ShutdownCamera ();
		}
	}
}

