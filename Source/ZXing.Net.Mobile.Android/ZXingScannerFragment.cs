using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;

namespace ZXing.Mobile
{
    public class ZXingScannerFragment : Fragment, IZXingScanner<View>, IScannerView
	{
	    public ZXingScannerFragment() 
        {
            UseCustomOverlayView = false;
	    }
            
		FrameLayout frame;

	    public override View OnCreateView (LayoutInflater layoutInflater, ViewGroup viewGroup, Bundle bundle)
		{
			frame = (FrameLayout)layoutInflater.Inflate(Resource.Layout.zxingscannerfragmentlayout, viewGroup, false);

            var layoutParams = new LinearLayout.LayoutParams (ViewGroup.LayoutParams.FillParent, ViewGroup.LayoutParams.FillParent);
            layoutParams.Weight = 1;

            try
            {
                scanner = new ZXingSurfaceView (this.Activity);

                frame.AddView(scanner, layoutParams);


                if (!UseCustomOverlayView)
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

            Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "ZXingScannerFragment->OnResume exit");

			return frame;
		}

        public override void OnStop ()
        {
            if (scanner != null)
            {
                scanner.ShutdownCamera();

                frame.RemoveView(scanner);
            }

            scanner = null;

            if (!UseCustomOverlayView)
                frame.RemoveView (zxingOverlay);
            else if (CustomOverlayView != null)
                frame.RemoveView (CustomOverlayView);
            
            base.OnStop ();
        }

		public View CustomOverlayView { get;set; }
        public bool UseCustomOverlayView { get; set ; }
		public MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public string TopText { get;set; }
		public string BottomText { get;set; }
		
		ZXingSurfaceView scanner;
		ZxingOverlayView zxingOverlay;

		public void Torch(bool on)
		{
			scanner.Torch(on);
		}
		
        public void AutoFocus()
        {
            scanner.AutoFocus();
        }

        public void AutoFocus(int x, int y)
		{
			scanner.AutoFocus();
		}

        Action<Result> scanCallback;
        bool scanImmediately = false;

        public void StartScanning (Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
        {            
            ScanningOptions = options;
            scanCallback = scanResultHandler;

            if (scanner == null) {
                scanImmediately = true;
                return;
            }

            scan ();
        }

        void scan ()
        {
            if (scanner != null)
                scanner.StartScanning (scanCallback, ScanningOptions);
        }

        public void StopScanning ()
        {
            scanner.StopScanning ();
        }

        public void PauseAnalysis ()
        {
            scanner.PauseAnalysis ();
        }

        public void ResumeAnalysis ()
        {
            scanner.ResumeAnalysis ();
        }

        public void ToggleTorch ()
        {
            scanner.ToggleTorch ();
        }

        public bool IsTorchOn {
            get {
                return scanner.IsTorchOn;
            }
        }

        public bool IsAnalyzing {
            get {
                return scanner.IsAnalyzing;
            }
        }

        public bool HasTorch {
            get {
                return scanner.HasTorch; 
            }
        }
	}
}

