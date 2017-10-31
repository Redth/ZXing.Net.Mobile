using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;

namespace ZXing.Mobile
{
    public class ZXingScannerFragment : Fragment, IZXingScanner<View>
    {
        private ZXingSurfaceView scanner;
        private ZxingOverlayView zxingOverlay;
        private FrameLayout frame;

        private Action<Result> scanCallback;
        private bool scanImmediately;

        public bool UseCustomOverlayView { get; set; }
        public View CustomOverlayView { get; set; }
        public MobileBarcodeScanningOptions ScanningOptions { get; set; }
        public string TopText { get; set; }
        public string BottomText { get; set; }

        public bool IsTorchOn => scanner?.IsTorchOn ?? false;
        public bool IsAnalyzing => scanner?.IsAnalyzing ?? false;
        public bool HasTorch => scanner?.HasTorch ?? false;

        public ZXingScannerFragment()
        {
            UseCustomOverlayView = false;
        }

        public override View OnCreateView(LayoutInflater layoutInflater, ViewGroup viewGroup, Bundle bundle)
        {
            frame = (FrameLayout)layoutInflater.Inflate(Resource.Layout.zxingscannerfragmentlayout, viewGroup, false);

            try
            {
                scanner = new ZXingSurfaceView(Activity, ScanningOptions);

                AddOverlayViewToFrame();

                if (scanImmediately)
                {
                    scanImmediately = false;
                    Scan();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create Surface View Failed: {ex}");
            }

            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "ZXingScannerFragment->OnResume exit");

            return frame;
        }

        private void AddOverlayViewToFrame()
        {
            var layoutParams = GetChildLayoutParams();
            frame.AddView(scanner, layoutParams);

            if (!UseCustomOverlayView)
            {
                if (zxingOverlay == null)
                {
                    zxingOverlay = new ZxingOverlayView(Activity)
                    {
                        TopText = TopText ?? "",
                        BottomText = BottomText ?? ""
                    };
                }

                frame.AddView(zxingOverlay, layoutParams);
            }
            else if (CustomOverlayView != null)
            {
                frame.AddView(CustomOverlayView, layoutParams);
            }
        }

        public override void OnStart()
        {
            base.OnStart();

            // won't be 0 if OnCreateView has been called before.
            if (frame.ChildCount == 0)
            {
                // reattach scanner and overlay views.
                AddOverlayViewToFrame();
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

        private LinearLayout.LayoutParams GetChildLayoutParams()
        {
            var layoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            layoutParams.Weight = 1;
            return layoutParams;
        }

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

        public void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
        {
            ScanningOptions = options;
            scanCallback = scanResultHandler;

            if (scanner == null)
            {
                scanImmediately = true;
                return;
            }

            Scan();
        }

        private void Scan()
        {
            scanner?.StartScanning(scanCallback, ScanningOptions);
        }

        public void StopScanning()
        {
            scanner?.StopScanning();
        }

        public void PauseAnalysis()
        {
            scanner?.PauseAnalysis();
        }

        public void ResumeAnalysis()
        {
            scanner?.ResumeAnalysis();
        }

        public void ToggleTorch()
        {
            scanner?.ToggleTorch();
        }
    }
}

