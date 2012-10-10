using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ZxingSharp.Mobile
{
    public class ZxingScanner : ZxingScannerBase
    {
        public ZxingScanner() : base()
        {
            
        }

        public override void StartScanning(ZxingScanningOptions options, Action<ZxingBarcodeResult> onFinished)
        {
            //Navigate: /ZxingSharp.WindowsPhone;component/Scan.xaml

            Scan.ScanningOptions = options;          
            Scan.FinishedAction = onFinished;
            Scan.UseCustomOverlay = this.UseCustomOverlay;
            Scan.CustomOverlay = this.CustomOverlay;
            Scan.TopText = TopText;
            Scan.BottomText = BottomText;

            ((Microsoft.Phone.Controls.PhoneApplicationFrame)Application.Current.RootVisual).Navigate(
                new Uri("/ZxingSharpWindowsPhone;component/WindowsPhone/Scan.xaml", UriKind.Relative));
        }

        public override void StopScanning()
        {
            Scan.RequestCancel();
        }

        public override void Torch(bool on)
        {
            Scan.RequestTorch(on);   
        }

        public override void ToggleTorch()
        {
            Scan.RequestToggleTorch();
        }

        public override bool IsTorchOn
        {
            get { return Scan.RequestIsTorchOn(); }
        }

        public override void AutoFocus()
        {
            Scan.RequestAutoFocus();
        }

        public System.Windows.UIElement CustomOverlay
        {
            get;
            set;
        }
    }
}
