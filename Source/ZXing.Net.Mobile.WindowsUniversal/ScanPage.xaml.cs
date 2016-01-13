using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ZXing.Mobile;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ZXing.Mobile
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScanPage : Page
    {
        private bool isNewInstance = false;

        public ScanPage()
        {
            isNewInstance = true;
            this.InitializeComponent();
        }

        public static MobileBarcodeScanningOptions ScanningOptions { get; set; }
        public static MobileBarcodeScannerBase Scanner { get; set; }
        public static UIElement CustomOverlay { get; set; }
        public static string TopText { get; set; }
        public static string BottomText { get; set; }
        public static bool UseCustomOverlay { get; set; }
        public static bool ContinuousScanning { get; set; }

        public static Result LastScanResult { get; set; }

        public static Action<Result> ResultFoundAction { get; set; }

        public static event Action<bool> OnRequestTorch;
        public static event Action OnRequestToggleTorch;
        public static event Action OnRequestAutoFocus;
        public static event Action OnRequestCancel;
        public static event Func<bool> OnRequestIsTorchOn;
        public static event Action OnRequestPauseAnalysis;
        public static event Action OnRequestResumeAnalysis;

        public static bool RequestIsTorchOn()
        {
            var evt = OnRequestIsTorchOn;
            return evt != null && evt();
        }

        public static void RequestTorch(bool on)
        {
            var evt = OnRequestTorch;
            if (evt != null)
                evt(on);
        }

        public static void RequestToggleTorch()
        {
            var evt = OnRequestToggleTorch;
            if (evt != null)
                evt();
        }

        public static void RequestAutoFocus()
        {
            var evt = OnRequestAutoFocus;
            if (evt != null)
                evt();
        }

        public static void RequestCancel()
        {
            var evt = OnRequestCancel;
            if (evt != null)
                evt();
        }

        public static void RequestPauseAnalysis()
        {
            var evt = OnRequestPauseAnalysis;
            if (evt != null)
                evt();
        }

        public static void RequestResumeAnalysis()
        {
            var evt = OnRequestResumeAnalysis;
            if (evt != null)
                evt();
        }

        void RequestAutoFocusHandler()
        {
            if (scannerControl != null)
                scannerControl.AutoFocus();
        }

        void RequestTorchHandler(bool on)
        {
            if (scannerControl != null)
                scannerControl.Torch(on);
        }

        void RequestToggleTorchHandler()
        {
            if (scannerControl != null)
                scannerControl.ToggleTorch();
        }

        async Task RequestCancelHandler()
        {
            if (scannerControl != null)
                await scannerControl.Cancel();
        }

        bool RequestIsTorchOnHandler()
        {
            if (scannerControl != null)
                return scannerControl.IsTorchOn;

            return false;
        }

        void RequestPauseAnalysisHandler()
        {
            if (scannerControl != null)
                scannerControl.PauseAnalysis();
        }

        void RequestResumeAnalysisHandler()
        {
            if (scannerControl != null)
                scannerControl.ResumeAnalysis();
        }
        
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            scannerControl.TopText = TopText;
            scannerControl.BottomText = BottomText;

            scannerControl.CustomOverlay = CustomOverlay;
            scannerControl.UseCustomOverlay = UseCustomOverlay;

            scannerControl.ScanningOptions = ScanningOptions;
            scannerControl.ContinuousScanning = ScanPage.ContinuousScanning;

            OnRequestAutoFocus += RequestAutoFocusHandler;
            OnRequestTorch += RequestTorchHandler;
            OnRequestToggleTorch += RequestToggleTorchHandler;
            OnRequestCancel += ScanPage_OnRequestCancel;
            OnRequestIsTorchOn += RequestIsTorchOnHandler;
            OnRequestPauseAnalysis += RequestPauseAnalysisHandler;
            OnRequestResumeAnalysis += RequestResumeAnalysisHandler;

            await scannerControl.StartScanningAsync(HandleResult, ScanningOptions);

            if (!isNewInstance && Frame.CanGoBack)
                Frame.GoBack();

            isNewInstance = false;

            base.OnNavigatedTo(e);
        }

        private async void ScanPage_OnRequestCancel()
        {
            await RequestCancelHandler();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            try
            {
                OnRequestAutoFocus -= RequestAutoFocusHandler;
                OnRequestTorch -= RequestTorchHandler;
                OnRequestToggleTorch -= RequestToggleTorchHandler;
                OnRequestCancel -= ScanPage_OnRequestCancel;
                OnRequestIsTorchOn -= RequestIsTorchOnHandler;
                OnRequestPauseAnalysis -= RequestPauseAnalysisHandler;
                OnRequestResumeAnalysis -= RequestResumeAnalysisHandler;

                await scannerControl.StopScanningAsync();
            }
            catch (Exception ex)
            {
                MobileBarcodeScanner.Log("OnNavigatingFrom Error: {0}", ex);
            }

            base.OnNavigatingFrom(e);
        }

        void HandleResult(ZXing.Result result)
        {
            LastScanResult = result;

            var evt = ResultFoundAction;
            if (evt != null)
                evt(LastScanResult);

            if (!ContinuousScanning)
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
               {
                   if (Frame.CanGoBack)
                       Frame.GoBack();
               });
                
            }
        }
    }
}
