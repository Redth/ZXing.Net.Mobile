using System;
using System.Threading.Tasks;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ZXing;

namespace ZXing.Mobile
{
    public class MobileBarcodeScanner : MobileBarcodeScannerBase
    {
        public MobileBarcodeScanner() : base()
        {
            
        }

        public override Task<Result> Scan(MobileBarcodeScanningOptions options)
        {
            return Task.Factory.StartNew(new Func<Result>(() =>
            {
                var scanResultResetEvent = new System.Threading.ManualResetEvent(false);

                Result result = null;

                //Navigate: /ZxingSharp.WindowsPhone;component/Scan.xaml

                ScanPage.ScanningOptions = options;
                ScanPage.FinishedAction = (r) => 
                {
                    result = r;
                    scanResultResetEvent.Set();
                };

                ScanPage.UseCustomOverlay = this.UseCustomOverlay;
                ScanPage.CustomOverlay = this.CustomOverlay;
                ScanPage.TopText = TopText;
                ScanPage.BottomText = BottomText;

                ((Microsoft.Phone.Controls.PhoneApplicationFrame)Application.Current.RootVisual).Navigate(
                    new Uri("/ZxingSharpWindowsPhone;component/WindowsPhone/Scan.xaml", UriKind.Relative));

                scanResultResetEvent.WaitOne();

                return result;
            }));            
        }

        public override void Cancel()
        {
            ScanPage.RequestCancel();
        }

        public override void Torch(bool on)
        {
            ScanPage.RequestTorch(on);   
        }

        public override void ToggleTorch()
        {
            ScanPage.RequestToggleTorch();
        }

        public override bool IsTorchOn
        {
            get { return ScanPage.RequestIsTorchOn(); }
        }

        public override void AutoFocus()
        {
            ScanPage.RequestAutoFocus();
        }

        public System.Windows.UIElement CustomOverlay
        {
            get;
            set;
        }
    }
}
