using Xamarin.Forms;
using ZXing.Mobile;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingScannerPage : ContentPage
    {
        ZXingScannerView zxing;
        ZXingDefaultOverlay defaultOverlay = null;
       
        public ZXingScannerPage (MobileBarcodeScanningOptions options = null, View customOverlay = null) : base ()
        {
            zxing = new ZXingScannerView
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Options = options,
                AutomationId = "zxingScannerView"
            };

            zxing.SetBinding( ZXingScannerView.IsTorchOnProperty, new Binding( nameof( IsTorchOn ) ) );
            zxing.SetBinding( ZXingScannerView.IsAnalyzingProperty, new Binding( nameof( IsAnalyzing ) ) );
            zxing.SetBinding( ZXingScannerView.IsScanningProperty, new Binding( nameof( IsScanning ) ) );
            zxing.SetBinding( ZXingScannerView.HasTorchProperty, new Binding( nameof( HasTorch ) ) );
            zxing.SetBinding( ZXingScannerView.ResultProperty, new Binding( nameof( Result ) ) );

            zxing.OnScanResult += (result) => {
                this.OnScanResult?.Invoke( result );
                //Device.BeginInvokeOnMainThread (() => eh (result));
            };

            if (customOverlay == null) {
                defaultOverlay = new ZXingDefaultOverlay () { AutomationId = "zxingDefaultOverlay" };

                defaultOverlay.SetBinding( ZXingDefaultOverlay.TopTextProperty, new Binding( nameof( DefaultOverlayTopText ) ) );
                defaultOverlay.SetBinding( ZXingDefaultOverlay.BottomTextProperty, new Binding( nameof( DefaultOverlayBottomText ) ) );
                defaultOverlay.SetBinding( ZXingDefaultOverlay.ShowFlashButtonProperty, new Binding( nameof( DefaultOverlayShowFlashButton ) ) );

                DefaultOverlayTopText = "Hold your phone up to the barcode";
                DefaultOverlayBottomText = "Scanning will happen automatically";
                DefaultOverlayShowFlashButton = HasTorch;

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

        #region Default Overlay Properties

        public static readonly BindableProperty DefaultOverlayTopTextProperty =
            BindableProperty.Create( nameof( DefaultOverlayTopText ), typeof( string ), typeof( ZXingScannerPage ), string.Empty );
        public string DefaultOverlayTopText
        {
            get { return ( string )GetValue( DefaultOverlayTopTextProperty ); }
            set { SetValue( DefaultOverlayTopTextProperty, value ); }
        }

        public static readonly BindableProperty DefaultOverlayBottomTextProperty =
            BindableProperty.Create( nameof( DefaultOverlayBottomText ), typeof( string ), typeof( ZXingScannerPage ), string.Empty );
        public string DefaultOverlayBottomText
        {
            get { return ( string )GetValue( DefaultOverlayBottomTextProperty ); }
            set { SetValue( DefaultOverlayBottomTextProperty, value ); }
        }

        public static readonly BindableProperty DefaultOverlayShowFlashButtonProperty =
            BindableProperty.Create( nameof( DefaultOverlayShowFlashButton ), typeof( bool ), typeof( ZXingScannerPage ), false );
        public bool DefaultOverlayShowFlashButton
        {
            get { return ( bool )GetValue( DefaultOverlayShowFlashButtonProperty ); }
            set { SetValue( DefaultOverlayShowFlashButtonProperty, value ); }
        }

        #endregion

        public delegate void ScanResultDelegate (ZXing.Result result);
        public event ScanResultDelegate OnScanResult;

        public View Overlay {
            get;
            private set;
        }

        #region Functions

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

        #endregion

        public static readonly BindableProperty IsTorchOnProperty =
            BindableProperty.Create( nameof( IsTorchOn ), typeof( bool ), typeof( ZXingScannerPage ), false );
        public bool IsTorchOn
        {
            get { return ( bool )GetValue( IsTorchOnProperty ); }
            set { SetValue( IsTorchOnProperty, value ); }
        }

        public static readonly BindableProperty IsAnalyzingProperty =
            BindableProperty.Create( nameof( IsAnalyzing ), typeof( bool ), typeof( ZXingScannerPage ), false );
        public bool IsAnalyzing
        {
            get { return ( bool )GetValue( IsAnalyzingProperty ); }
            set { SetValue( IsAnalyzingProperty, value ); }
        }

        public static readonly BindableProperty IsScanningProperty =
            BindableProperty.Create( nameof( IsScanning ), typeof( bool ), typeof( ZXingScannerPage ), false );
        public bool IsScanning
        {
            get { return ( bool )GetValue( IsScanningProperty ); }
            set { SetValue( IsScanningProperty, value ); }
        }

        public static readonly BindableProperty HasTorchProperty =
            BindableProperty.Create( nameof( HasTorch ), typeof( bool ), typeof( ZXingScannerPage ), false );
        public bool HasTorch {
            get { return ( bool )GetValue( HasTorchProperty ); }
            set { SetValue( HasTorchProperty, value ); }
        }

        public static readonly BindableProperty ResultProperty =
            BindableProperty.Create( nameof( Result ), typeof( Result ), typeof( ZXingScannerPage ), default( Result ) );
        public Result Result
        {
            get { return ( Result )GetValue( ResultProperty ); }
            set { SetValue( ResultProperty, value ); }
        }
    }
}

