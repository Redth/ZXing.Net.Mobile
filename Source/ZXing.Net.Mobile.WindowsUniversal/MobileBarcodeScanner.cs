using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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

        private Frame CurrentFrame
        {
            get
            {
                var currentFrame = RootFrame ??
                                   Window.Current.Content as Frame ??
                                   ((FrameworkElement) Window.Current.Content).GetFirstChildOfType<Frame>();

                var currentPage = currentFrame.Content as Page;
                if (currentPage != null && currentPage.NavigationCacheMode == NavigationCacheMode.Disabled)
                    Log("WARNING: if you're having trouble updating the UI of a Page after scanning is completed, " +
                        "you may need to set {0}=\"Enabled\" within your Page's XAML <Page ... /> element",
                        nameof(Page.NavigationCacheMode));

                return currentFrame;
            }
        }

        private CoreDispatcher CurrentDispatcher
        {
            get { return Dispatcher ?? Window.Current.Dispatcher; }
        }

        public override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {
            var frame = CurrentFrame;
            var dispatcher = CurrentDispatcher;

            ScanPage.ScanningOptions = options;
            ScanPage.ResultFoundAction = scanHandler;

            ScanPage.UseCustomOverlay = UseCustomOverlay;
            ScanPage.CustomOverlay = CustomOverlay;
            ScanPage.TopText = TopText;
            ScanPage.BottomText = BottomText;
            ScanPage.ContinuousScanning = true;

            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                frame.Navigate(typeof(ScanPage));
            });
        }

        public override Task<Result> Scan(MobileBarcodeScanningOptions options)
        {
            var frame = CurrentFrame;
            var dispatcher = CurrentDispatcher;
            return Task.Run(() =>
            {
                var scanResultResetEvent = new System.Threading.ManualResetEvent(false);

                Result result = null;

                ScanPage.ScanningOptions = options;
                ScanPage.ResultFoundAction = r => 
                {
                    result = r;
                    scanResultResetEvent.Set();
                };

                ScanPage.UseCustomOverlay = UseCustomOverlay;
                ScanPage.CustomOverlay = CustomOverlay;
                ScanPage.TopText = TopText;
                ScanPage.BottomText = BottomText;
                ScanPage.ContinuousScanning = false;

                dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    frame.Navigate(typeof(ScanPage));
                });
                
                scanResultResetEvent.WaitOne();

                return result;
            });            
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

        public UIElement CustomOverlay { get; set; }

        internal static void Log(string message, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine("ZXING: " + message, args);
        }
    }
}
