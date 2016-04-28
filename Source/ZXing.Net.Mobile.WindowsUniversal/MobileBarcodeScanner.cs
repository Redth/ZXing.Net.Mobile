using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ZXing.Mobile
{
    public class MobileBarcodeScanner : MobileBarcodeScannerBase
    {
        public MobileBarcodeScanner () : base ()
        {

        }

        public MobileBarcodeScanner(CoreDispatcher dispatcher) : base()
        {
            Dispatcher = dispatcher;
        }

        public CoreDispatcher Dispatcher { get; set; }

        public Frame RootFrame { get; set; }

        public async override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {
            var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement) Window.Current.Content).GetFirstChildOfType<Frame>();
            var dispatcher = Dispatcher ?? rootFrame.Dispatcher;

            ScanPage.ScanningOptions = options;
            ScanPage.ResultFoundAction = scanHandler;

            ScanPage.UseCustomOverlay = this.UseCustomOverlay;
            ScanPage.CustomOverlay = this.CustomOverlay;
            ScanPage.TopText = TopText;
            ScanPage.BottomText = BottomText;
            ScanPage.ContinuousScanning = true;

            await rootFrame.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                rootFrame.Navigate(typeof(ScanPage));
            });
        }

        public override Task<Result> Scan(MobileBarcodeScanningOptions options)
        {
            var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement) Window.Current.Content).GetFirstChildOfType<Frame>();
            var dispatcher = Dispatcher ?? rootFrame.Dispatcher;

            return Task.Factory.StartNew(new Func<Result>(() =>
            {
                var scanResultResetEvent = new System.Threading.ManualResetEvent(false);

                Result result = null;

                ScanPage.ScanningOptions = options;
                ScanPage.ResultFoundAction = (r) =>
                {
                    result = r;
                    scanResultResetEvent.Set();
                };

                ScanPage.UseCustomOverlay = this.UseCustomOverlay;
                ScanPage.CustomOverlay = this.CustomOverlay;
                ScanPage.TopText = TopText;
                ScanPage.BottomText = BottomText;

                dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    rootFrame.Navigate(typeof(ScanPage));
                });

                scanResultResetEvent.WaitOne();

                return result;
            }));            
        }

        public override void Cancel()
        {
            ScanPage.RequestCancel();
        }

        public override void Torch(bool on)
        {
            ScanPage.RequestTorch(on);   
        }

        public override void ToggleTorch()
        {
            ScanPage.RequestToggleTorch();
        }

        public override bool IsTorchOn
        {
            get { return ScanPage.RequestIsTorchOn(); }
        }

        public override void AutoFocus()
        {
            ScanPage.RequestAutoFocus();
        }

        public override void PauseAnalysis()
        {
            ScanPage.RequestPauseAnalysis();
        }

        public override void ResumeAnalysis()
        {
            ScanPage.RequestResumeAnalysis();
        }

        public UIElement CustomOverlay
        {
            get;
            set;
        }

        internal static void Log(string message, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(message, args);
        }
    }
}
