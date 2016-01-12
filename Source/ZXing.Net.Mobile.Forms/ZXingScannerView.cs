using System;
using Xamarin.Forms;
using ZXing.Mobile;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingScannerView : View
    {
        public delegate void ScanResultDelegate (ZXing.Result result);
        public event ScanResultDelegate OnScanResult;

        public IScannerView InternalNativeScannerImplementation { get; set; }

        public ZXingScannerView ()
        {
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
        }

        public void RaiseScanResult (ZXing.Result result)
        {
            var e = this.OnScanResult;
            if (e != null)
                e (result);
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


        public static readonly BindableProperty OptionsProperty =
            BindableProperty.Create<ZXingScannerView, MobileBarcodeScanningOptions> (
                p => p.Options, 
                defaultValue: MobileBarcodeScanningOptions.Default, 
                defaultBindingMode: BindingMode.TwoWay);
        
        public MobileBarcodeScanningOptions Options {
            get { return (MobileBarcodeScanningOptions)GetValue (OptionsProperty); }
            set { SetValue (OptionsProperty, value); }
        }

        public static readonly BindableProperty IsScanningProperty =
            BindableProperty.Create<ZXingScannerView, bool> (
                p => p.IsScanning, 
                defaultValue: false, 
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: (bindable, oldValue, newValue) => {
                    try {
                        if (bindable == null)
                            return;
                        var scannerView = (ZXingScannerView)bindable;
                        if (newValue && !scannerView.isScanning) {
                            if (scannerView.InternalNativeScannerImplementation != null) {
                                scannerView.isScanning = true;


                                scannerView.InternalNativeScannerImplementation.StartScanning (
                                        scannerView.RaiseScanResult, scannerView.Options);
                            }
                        } else if (!newValue && scannerView.isScanning) {
                            scannerView.isScanning = false;
                            if (scannerView.InternalNativeScannerImplementation != null)
                                scannerView.InternalNativeScannerImplementation.StopScanning ();
                        }
                    } catch {}
                });

        bool isScanning = false;
        public bool IsScanning {
            get { return isScanning; }
            set { SetValue (IsScanningProperty, value); }
        }

        public static readonly BindableProperty IsTorchOnProperty =
            BindableProperty.Create<ZXingScannerView, bool> (
                p => p.IsTorchOn, 
                defaultValue: false, 
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: (bindable, oldValue, newValue) => {
                    try {
                        if (bindable == null)
                            return;
                        var scannerView = (ZXingScannerView)bindable;
                        if (scannerView.InternalNativeScannerImplementation != null)
                            scannerView.InternalNativeScannerImplementation.Torch (newValue);
                    } catch { }
                });

        public bool IsTorchOn {
            get { return InternalNativeScannerImplementation != null && InternalNativeScannerImplementation.IsTorchOn; }
            set { SetValue (IsTorchOnProperty, value); }                
        }


        public static readonly BindableProperty HasTorchProperty =
            BindableProperty.Create<ZXingScannerView, bool> (
                p => p.HasTorch, 
                defaultValue: false, 
                defaultBindingMode: BindingMode.OneWay);

        public bool HasTorch {
            get { return InternalNativeScannerImplementation != null && InternalNativeScannerImplementation.HasTorch; }
        }


        public static readonly BindableProperty IsAnalyzingProperty = 
            BindableProperty.Create<ZXingScannerView, bool> (
                p => p.IsAnalyzing,
                defaultValue: false,
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: 
                (bindable, oldValue, newValue) => {
                    try { 
                        if (bindable == null)
                            return;
                        var scannerView = (ZXingScannerView)bindable;
                        if (scannerView.InternalNativeScannerImplementation != null) {
                            if (newValue && !scannerView.InternalNativeScannerImplementation.IsAnalyzing)
                                scannerView.InternalNativeScannerImplementation.ResumeAnalysis ();
                            else if (!newValue && scannerView.InternalNativeScannerImplementation.IsAnalyzing)
                                scannerView.InternalNativeScannerImplementation.PauseAnalysis ();
                        }
                    } catch { }
                });

        public bool IsAnalyzing {
            get { return InternalNativeScannerImplementation != null && InternalNativeScannerImplementation.IsAnalyzing; }
            set { SetValue (IsAnalyzingProperty, value); }
        }
    }
}

