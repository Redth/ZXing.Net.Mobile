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
            //this.Dispatcher = Windows.Current.Dispatcher;
        }

        public MobileBarcodeScanner(CoreDispatcher dispatcher) : base()
        {
            this.Dispatcher = dispatcher;
        }

        public CoreDispatcher Dispatcher { get; set; }

        public Frame RootFrame { get; set; }

        public override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {
            //Navigate: /ZxingSharp.WindowsPhone;component/Scan.xaml

            // Too much problem on it, such like: can't return correctly after finish. So I decide use new frame.
            //var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement) Window.Current.Content).GetFirstChildOfType<Frame>();

            ScanPage.LastFrame = Window.Current.Content;
            var rootFrame = RootFrame ?? (Window.Current.Content = new Frame()) as Frame;
            var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

            ScanPage.ScanningOptions = options;
            ScanPage.ResultFoundAction = scanHandler;

            ScanPage.UseCustomOverlay = this.UseCustomOverlay;
            ScanPage.CustomOverlay = this.CustomOverlay;
            ScanPage.TopText = TopText;
            ScanPage.BottomText = BottomText;
            ScanPage.CancelButtonText = CancelButtonText;
            ScanPage.ContinuousScanning = true;
            
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                rootFrame.Navigate(typeof(ScanPage));
            });

        }

        public override Task<Result> Scan(MobileBarcodeScanningOptions options)
        {
            // Too much problem on it, such like: can't return correctly after finish. So I decide use new frame.
            //var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement) Window.Current.Content).GetFirstChildOfType<Frame>();

            ScanPage.LastFrame = Window.Current.Content;
            var rootFrame = RootFrame ?? (Window.Current.Content = new Frame()) as Frame;
            var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

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
                ScanPage.CancelButtonText = CancelButtonText;

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
