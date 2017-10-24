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

        internal ScanPage ScanPage { get; set; }

        public CoreDispatcher Dispatcher { get; set; }

        public Frame RootFrame { get; set; }

        public override async void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {
            //Navigate: /ZxingSharp.WindowsPhone;component/Scan.xaml
            var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement) Window.Current.Content).GetFirstChildOfType<Frame>();
            var dispatcher = Dispatcher ?? Window.Current.Dispatcher;
            
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                rootFrame.Navigate(typeof(ScanPage), new ScanPageNavigationParameters
                {
                    Options = options,
                    ResultHandler = scanHandler,
                    Scanner = this,
                    ContinuousScanning = true
                });
            });
        }

        public override async Task<Result> Scan(MobileBarcodeScanningOptions options)
        {
            var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement) Window.Current.Content).GetFirstChildOfType<Frame>();
            var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

            var tcsScanResult = new TaskCompletionSource<Result>();

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                rootFrame.Navigate(typeof(ScanPage), new ScanPageNavigationParameters
                {
                    Options = options,
                    ResultHandler = r =>
                    {
                        tcsScanResult.SetResult(r);
                    },
                    Scanner = this,
                    ContinuousScanning = false
                });
            });
            
            var result = await tcsScanResult.Task;

            await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (rootFrame.CanGoBack)
                    rootFrame.GoBack();
            });

            return result;
        }

        public override async void Cancel()
        {
            var rootFrame = RootFrame ?? Window.Current.Content as Frame ?? ((FrameworkElement)Window.Current.Content).GetFirstChildOfType<Frame>();
            var dispatcher = Dispatcher ?? Window.Current.Dispatcher;

            ScanPage?.Cancel();

            await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (rootFrame.CanGoBack)
                    rootFrame.GoBack();
            });
        }

        public override void Torch(bool on)
        {
            ScanPage?.Torch(on);
        }

        public override void ToggleTorch()
        {
            ScanPage?.ToggleTorch();
        }

        public override bool IsTorchOn
        {
            get { return ScanPage?.IsTorchOn ?? false; }
        }

        public override void AutoFocus()
        {
            ScanPage?.AutoFocus();
        }

        public override void PauseAnalysis()
        {
            ScanPage?.PauseAnalysis();
        }

        public override void ResumeAnalysis()
        {
            ScanPage?.ResumeAnalysis();
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
