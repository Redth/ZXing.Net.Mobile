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

            var layoutParams = getChildLayoutParams();

            try
            {
                scanner = new ZXingSurfaceView (this.Activity, ScanningOptions);

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

        public override void OnStart()
        {
            base.OnStart();
            // won't be 0 if OnCreateView has been called before.
            if (frame.ChildCount == 0)
            {
                var layoutParams = getChildLayoutParams();
                // reattach scanner and overlay views.
                frame.AddView(scanner, layoutParams);

                if (!UseCustomOverlayView)
                    frame.AddView (zxingOverlay, layoutParams);
                else if (CustomOverlayView != null)
                    frame.AddView (CustomOverlayView, layoutParams);
            }
        }

        public override void OnStop()
        {
            if (scanner != null)
            {
                scanner.StopScanning();

                frame.RemoveView(scanner);
            }

            if (!UseCustomOverlayView)
                frame.RemoveView(zxingOverlay);
            else if (CustomOverlayView != null)
                frame.RemoveView(CustomOverlayView);

            base.OnStop();
        }

        private LinearLayout.LayoutParams getChildLayoutParams()
        {
            var layoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            layoutParams.Weight = 1;
            return layoutParams;
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
			scanner?.Torch(on);
		}
		
        public void AutoFocus()
        {
            scanner?.AutoFocus();
        }

        public void AutoFocus(int x, int y)
		{
			scanner?.AutoFocus(x, y);
		}

        Action<Result> scanCallback;
        //bool scanImmediately = false;

        public void StartScanning (Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
        {            
            ScanningOptions = options;
            scanCallback = scanResultHandler;

            if (scanner == null) {
                //scanImmediately = true;
                return;
            }

            scan ();
        }

        void scan ()
        {
            scanner?.StartScanning (scanCallback, ScanningOptions);
        }

        public void StopScanning ()
        {
            scanner?.StopScanning ();
        }

        public void PauseAnalysis ()
        {
            scanner?.PauseAnalysis ();
        }

        public void ResumeAnalysis ()
        {
            scanner?.ResumeAnalysis ();
        }

        public void ToggleTorch ()
        {
            scanner?.ToggleTorch ();
        }

        public bool IsTorchOn {
            get {
                return scanner?.IsTorchOn ?? false;
            }
        }

        public bool IsAnalyzing {
            get {
                return scanner?.IsAnalyzing ?? false;
            }
        }

        public bool HasTorch {
            get {
                return scanner?.HasTorch ?? false;
            }
        }
	}
}

