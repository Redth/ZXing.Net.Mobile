using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using ZXing;

namespace ZXing.Mobile
{
	public partial class ScanPage : PhoneApplicationPage
	{
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
        public static Action OnRequestPauseAnalysis;
        public static Action OnRequestResumeAnalysis;
        
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

		public ScanPage()
		{
			InitializeComponent();
		    isNewInstance = true;
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

        void RequestCancelHandler()
        {
            if (scannerControl != null)
                scannerControl.Cancel();
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

        bool RequestIsTorchOnHandler()
        {
            if (scannerControl != null)
                return scannerControl.IsTorchOn;

            return false;
        }

		protected override void OnNavigatedTo(NavigationEventArgs e)
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
            OnRequestCancel += RequestCancelHandler;
            OnRequestIsTorchOn += RequestIsTorchOnHandler;
            OnRequestPauseAnalysis += RequestPauseAnalysisHandler;
            OnRequestResumeAnalysis += RequestResumeAnalysisHandler;
            
            scannerControl.StartScanning(HandleResult, ScanningOptions);

            if (!isNewInstance && !isDeactivated && NavigationService.CanGoBack)
                NavigationService.GoBack();
            
            isNewInstance = false;

            base.OnNavigatedTo(e);
        }

	private bool isNewInstance = false;
	private bool isDeactivated = false;
	    
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            try 
            {
            	isDeactivated = e.NavigationMode != NavigationMode.Back;
                OnRequestAutoFocus -= RequestAutoFocusHandler;
                OnRequestTorch -= RequestTorchHandler;
                OnRequestToggleTorch -= RequestToggleTorchHandler;
                OnRequestCancel -= RequestCancelHandler;
                OnRequestIsTorchOn -= RequestIsTorchOnHandler;
                OnRequestPauseAnalysis -= RequestPauseAnalysisHandler;
                OnRequestResumeAnalysis -= RequestResumeAnalysisHandler;

                scannerControl.StopScanning();

                if (!ContinuousScanning)
                    RaiseEmptyResult();
            }
            catch (Exception ex) {
                MobileBarcodeScanner.Log("OnNavigatingFrom Error: {0}", ex);
            }

            base.OnNavigatingFrom(e);
        }

        void RaiseEmptyResult()
        {
            LastScanResult = null;

            var evt = ResultFoundAction;
            if (evt != null)
                evt(LastScanResult);
        }

        void HandleResult(ZXing.Result result)
        {
            LastScanResult = result;

            var evt = ResultFoundAction;
            if (evt != null)
                evt(LastScanResult);

            if (!ContinuousScanning)
            {
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }
	}
}
