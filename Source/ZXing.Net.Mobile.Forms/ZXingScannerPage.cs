using System;
using Xamarin.Forms;
using ZXing.Mobile;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingScannerPage : ContentPage
    {
        ZXingScannerView zxing;
        ZXingDefaultOverlay defaultOverlay = null;
       
        public ZXingScannerPage (ZXing.Mobile.MobileBarcodeScanningOptions options = null, View customOverlay = null) : base ()
        {
            zxing = new ZXingScannerView
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };
            zxing.OnScanResult += (result) => {
                var eh = this.OnScanResult;
                if (eh != null)
                    Device.BeginInvokeOnMainThread (() => eh (result));
            };

            if (customOverlay == null) {
                defaultOverlay = new ZXingDefaultOverlay {
                    TopText = "Hold your phone up to the barcode",
                    BottomText = "Scanning will happen automatically",
                    ShowFlashButton = zxing.HasTorch,
                };
                defaultOverlay.FlashButtonClicked += (sender, e) => {
                    zxing.IsTorchOn = !zxing.IsTorchOn;
                };
                Overlay = defaultOverlay;
            } else {
                Overlay = customOverlay;
            }

            var grid = new Grid
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };
            grid.Children.Add(zxing);
            grid.Children.Add(Overlay);

            // The root page of your application
            Content = grid;
        }

        public string DefaultOverlayTopText {
            get {
                return defaultOverlay == null ? string.Empty : defaultOverlay.TopText;
            }
            set {
                if (defaultOverlay != null)
                    defaultOverlay.TopText = value;
            }
        }
        public string DefaultOverlayBottomText {
            get {
                return defaultOverlay == null ? string.Empty : defaultOverlay.BottomText;
            }
            set {
                if (defaultOverlay != null)
                    defaultOverlay.BottomText = value;
            }
        }
        public bool DefaultOverlayShowFlashButton {
            get {
                return defaultOverlay == null ? false : defaultOverlay.ShowFlashButton;
            }
            set {
                if (defaultOverlay != null)
                    defaultOverlay.ShowFlashButton = value;
            }
        }

        public delegate void ScanResultDelegate (ZXing.Result result);
        public event ScanResultDelegate OnScanResult;

        public View Overlay {
            get;
            private set;
        }

        public void ToggleTorch ()
        {
            if (zxing != null)
                zxing.ToggleTorch ();
        }

        protected override void OnAppearing ()
        {
            base.OnAppearing ();

            zxing.IsScanning = true;
        }

        protected override void OnDisappearing ()
        {
            zxing.IsScanning = false;

            base.OnDisappearing ();
        }

        public void PauseAnalysis ()
        {
            if (zxing != null)
                zxing.IsAnalyzing = false;
        }

        public void ResumeAnalysis ()
        {
            if (zxing != null)
                zxing.IsAnalyzing = true;
        }

        public void AutoFocus ()
        {
            if (zxing != null)
                zxing.AutoFocus ();
        }

        public void AutoFocus (int x, int y)
        {
            if (zxing != null)
                zxing.AutoFocus (x, y);
        }

        public bool IsTorchOn {
            get {
                return zxing == null ? false : zxing.IsTorchOn;
            }
            set {
                if (zxing != null)
                    zxing.IsTorchOn = value;
            }
        }

        public bool IsAnalyzing {
            get {
                return zxing == null ? false : zxing.IsAnalyzing;
            }
            set {
                if (zxing != null)
                    zxing.IsAnalyzing = value;
            }
        }

        public bool HasTorch {
            get {
                return zxing == null ? false : zxing.HasTorch;
            }
        }
    }
}

