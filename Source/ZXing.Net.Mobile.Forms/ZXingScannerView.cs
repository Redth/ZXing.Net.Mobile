using System;
using Xamarin.Forms;
using ZXing.Mobile;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingScannerView : View, IScannerView
    {
        public delegate void ScanResultDelegate (ZXing.Result result);
        public event ScanResultDelegate OnScanResult;

        public IScannerView InternalNativeScannerImplementation { get; set; }

        public ZXingScannerView ()
        {
            //IsClippedToBounds = true;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
        }

        public void RaiseScanResult (ZXing.Result result)
        {
            var e = this.OnScanResult;
            if (e != null)
                e (result);
        }

        public void StartScanning (MobileBarcodeScanningOptions options = null)
        {
            if (InternalNativeScannerImplementation != null) {
                InternalNativeScannerImplementation.StartScanning (result => {
                    var h = OnScanResult;
                    if (h != null)
                        h (result);                    
                }, options);
            }
        }

        public void StopScanning ()
        {
            if (InternalNativeScannerImplementation != null)
                InternalNativeScannerImplementation.StopScanning ();
        }

        public void ToggleTorch ()
        {
            if (InternalNativeScannerImplementation != null)
                InternalNativeScannerImplementation.ToggleTorch ();
        }

        public void AutoFocus ()
        {
            if (InternalNativeScannerImplementation != null)
                InternalNativeScannerImplementation.AutoFocus ();
        }

        public void AutoFocus (int x, int y)
        {
            if (InternalNativeScannerImplementation != null)
                InternalNativeScannerImplementation.AutoFocus (x, y);
        }

        public void StartScanning (Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
        {
            if (InternalNativeScannerImplementation != null) {
                InternalNativeScannerImplementation.StartScanning (result => {
                    var h = OnScanResult;
                    if (h != null)
                        h (result);
                    if (scanResultHandler != null)
                        scanResultHandler (result);
                }, options);
            }
        }

        public void PauseAnalysis ()
        {
            if (InternalNativeScannerImplementation != null)
                InternalNativeScannerImplementation.PauseAnalysis ();
        }

        public void ResumeAnalysis ()
        {
            if (InternalNativeScannerImplementation != null)
                InternalNativeScannerImplementation.ResumeAnalysis ();
        }

        public void Torch (bool on)
        {
            if (InternalNativeScannerImplementation != null)
                InternalNativeScannerImplementation.Torch (on);
        }

        public bool IsTorchOn {
            get {
                if (InternalNativeScannerImplementation != null)
                    return InternalNativeScannerImplementation.IsTorchOn;

                return false;
            }
            set {
                if (InternalNativeScannerImplementation != null)
                    InternalNativeScannerImplementation.Torch (value);
            }
        }

        public bool IsAnalyzing {
            get {
                if (InternalNativeScannerImplementation != null)
                    return InternalNativeScannerImplementation.IsAnalyzing;

                return false;
            }
            set {
                if (InternalNativeScannerImplementation != null) {
                    if (value && !IsAnalyzing)
                        InternalNativeScannerImplementation.ResumeAnalysis ();
                    else if (!value && IsAnalyzing)
                        InternalNativeScannerImplementation.PauseAnalysis ();
                }
            }
        }
    }
}

