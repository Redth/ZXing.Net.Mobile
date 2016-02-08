using System;
using Xamarin.Forms;
using ZXing.Mobile;
using System.Threading;
using System.Threading.Tasks;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingScannerView : View
    {
        public delegate void ScanResultDelegate (ZXing.Result result);
        public event ScanResultDelegate OnScanResult;

        public event Action<int, int> AutoFocusRequested;

        public ZXingScannerView ()
        {
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
        }

        public void RaiseScanResult (Result result)
        {
            var e = this.OnScanResult;
            if (e != null)
                e (result);
        }


        public void ToggleTorch ()
        {
            IsTorchOn = !IsTorchOn;
        }

        public void AutoFocus ()
        {
            AutoFocusRequested?.Invoke (-1, -1);
        }

        public void AutoFocus (int x, int y)
        {
            AutoFocusRequested?.Invoke (x, y);
        }


        public static readonly BindableProperty OptionsProperty =
            BindableProperty.Create<ZXingScannerView, MobileBarcodeScanningOptions> (
                p => p.Options, MobileBarcodeScanningOptions.Default);
        
        public MobileBarcodeScanningOptions Options {
            get { return (MobileBarcodeScanningOptions)GetValue (OptionsProperty); }
            set { SetValue (OptionsProperty, value); }
        }

        public static readonly BindableProperty IsScanningProperty =
            BindableProperty.Create<ZXingScannerView, bool> (p => p.IsScanning, false);

        public bool IsScanning {
            get { return (bool)GetValue (IsScanningProperty); }
            set { SetValue (IsScanningProperty, value); }
        }

        public static readonly BindableProperty IsTorchOnProperty =
            BindableProperty.Create<ZXingScannerView, bool> (p => p.IsTorchOn, false);
        public bool IsTorchOn {
            get { return (bool)GetValue (IsTorchOnProperty); }
            set { SetValue (IsTorchOnProperty, value); }                
        }


        public static readonly BindableProperty HasTorchProperty =
            BindableProperty.Create<ZXingScannerView, bool> (p => p.HasTorch, false);
        public bool HasTorch {
            get { return (bool)GetValue (HasTorchProperty); }
        }


        public static readonly BindableProperty IsAnalyzingProperty = 
            BindableProperty.Create<ZXingScannerView, bool> (p => p.IsAnalyzing, false);

        public bool IsAnalyzing {
            get { return (bool)GetValue (IsAnalyzingProperty); }
            set { SetValue (IsAnalyzingProperty, value); }
        }
    }
}

