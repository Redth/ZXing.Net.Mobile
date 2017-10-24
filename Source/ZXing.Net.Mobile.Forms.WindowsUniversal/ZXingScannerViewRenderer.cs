using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.WindowsUniversal;
using Xamarin.Forms.Platform.UWP;
using System.ComponentModel;
using System.Reflection;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsUniversal
{
    //[Preserve(AllMembers = true)]
    public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingScannerControl>
    {
        public static void Init ()
        {
            // Cause the assembly to load
        }

        protected ZXingScannerView formsView;

        protected ZXing.Mobile.ZXingScannerControl zxingControl;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            formsView = Element;

            if (formsView != null && zxingControl == null)
            {
                formsView.AutoFocusRequested += FormsView_AutoFocusRequested;

                zxingControl = new ZXing.Mobile.ZXingScannerControl();
                zxingControl.ContinuousScanning = true;
                zxingControl.UseCustomOverlay = true;

                base.SetNativeControl(zxingControl);

                if (formsView.IsScanning)
                    zxingControl.StartScanning(formsView.RaiseScanResult, formsView.Options);

                if (!formsView.IsAnalyzing)
                    zxingControl.PauseAnalysis();

                if (formsView.IsTorchOn)
                    zxingControl.Torch(formsView.IsTorchOn);
            }

            // Shut the scanner down if necessary
            if (formsView == null && e.NewElement == null && zxingControl != null)
            {
                zxingControl.StopScanning();
            }

            base.OnElementChanged(e);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (zxingControl == null)
                return;

            switch (e.PropertyName)
            {
                case nameof(ZXingScannerView.IsTorchOn):
                    zxingControl.Torch(formsView.IsTorchOn);
                    break;
                case nameof(ZXingScannerView.IsScanning):
                    if (formsView.IsScanning)
                        zxingControl.StartScanning(formsView.RaiseScanResult, formsView.Options);
                    else
                        zxingControl.StopScanning();
                    break;
                case nameof(ZXingScannerView.IsAnalyzing):
                    if (formsView.IsAnalyzing)
                        zxingControl.ResumeAnalysis();
                    else
                        zxingControl.PauseAnalysis();
                    break;
            }
        }

        private void FormsView_AutoFocusRequested(int x, int y)
        {
           zxingControl.AutoFocus(x, y);
        }

        protected override async void OnDisconnectVisualChildren()
        {
            await zxingControl?.StopScanningAsync();
            base.OnDisconnectVisualChildren();
        }
    }
}

