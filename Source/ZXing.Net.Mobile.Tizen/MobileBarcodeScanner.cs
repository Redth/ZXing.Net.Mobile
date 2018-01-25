using ElmSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
    public class MobileBarcodeScanner : MobileBarcodeScannerBase
    {
        public override bool IsTorchOn => ZxingScannerWindow.IsTorchOn;
        public Container CustomOverlay {set; get;}
        public Window MainWindow { get; internal set; }
        private ZxingScannerWindow ZxingScannerWindow;
        public MobileBarcodeScanner() : base()
        {
            ZxingScannerWindow = new ZxingScannerWindow();
            MainWindow = ZxingScannerWindow;
        }
        public override void AutoFocus()
        {
            ZxingScannerWindow?.AutoFocus();
        }

        public override void Cancel()
        {
            ZxingScannerWindow.Unrealize();
        }

        public override void PauseAnalysis()
        {
            ZxingScannerWindow.PauseAnalysis();
        }

        public override void ResumeAnalysis()
        {
            ZxingScannerWindow.ResumeAnalysis();
        }

        public override Task<Result> Scan(MobileBarcodeScanningOptions options)
        {                       
            var task = Task.Factory.StartNew(() =>
            {
                var waitScanResetEvent = new ManualResetEvent(false);
                Result result = null;               
                ZxingScannerWindow.UseCustomOverlayView = UseCustomOverlay;
                ZxingScannerWindow.CustomOverlayView = CustomOverlay;
                ZxingScannerWindow.ScanningOptions = options;
                ZxingScannerWindow.ScanContinuously = false;
                ZxingScannerWindow.TopText = TopText;
                ZxingScannerWindow.BottomText = BottomText;

                ZxingScannerWindow.ScanCompletedHandler = (Result r) =>
                {
                    result = r;
                    waitScanResetEvent.Set();
                };
                ZxingScannerWindow.Show();
                waitScanResetEvent.WaitOne();
                return result;
            });
            return task;
        }

        public override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {
            ZxingScannerWindow.UseCustomOverlayView = this.UseCustomOverlay;
            ZxingScannerWindow.CustomOverlayView = CustomOverlay;
            ZxingScannerWindow.ScanningOptions = options;
            ZxingScannerWindow.ScanContinuously = true;
            ZxingScannerWindow.TopText = TopText;
            ZxingScannerWindow.BottomText = BottomText;
            ZxingScannerWindow.ScanCompletedHandler = (Result r) =>
            {
                scanHandler?.Invoke(r);
            };
            ZxingScannerWindow.Show();
        }

        public override void ToggleTorch()
        {
            ZxingScannerWindow?.ToggleTorch();
        }

        public override void Torch(bool on)
        {
            ZxingScannerWindow?.Torch(on);
        }
    }
}
