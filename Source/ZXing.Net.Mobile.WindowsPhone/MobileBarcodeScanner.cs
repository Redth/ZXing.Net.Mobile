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
        public MobileBarcodeScanner () : base ()
        {
            this.Dispatcher = Deployment.Current.Dispatcher;
        }

        public MobileBarcodeScanner(System.Windows.Threading.Dispatcher dispatcher) : base()
        {
			this.Dispatcher = dispatcher;
        }

		public System.Windows.Threading.Dispatcher Dispatcher { get; set; }

        public override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {
            //Navigate: /ZxingSharp.WindowsPhone;component/Scan.xaml

            ScanPage.ScanningOptions = options;
            ScanPage.ResultFoundAction = (r) =>
            {
                scanHandler(r);
            };

            ScanPage.UseCustomOverlay = this.UseCustomOverlay;
            ScanPage.CustomOverlay = this.CustomOverlay;
            ScanPage.TopText = TopText;
            ScanPage.BottomText = BottomText;
            ScanPage.ContinuousScanning = true;

            Dispatcher.BeginInvoke(() =>
            {
                ((Microsoft.Phone.Controls.PhoneApplicationFrame)Application.Current.RootVisual).Navigate(
                    new Uri("/ZXingNetMobile;component/ScanPage.xaml", UriKind.Relative));
            });
        }

        public override Task<Result> Scan(MobileBarcodeScanningOptions options)
        {
            return Task.Factory.StartNew(new Func<Result>(() =>
            {
                var scanResultResetEvent = new System.Threading.ManualResetEvent(false);

                Result result = null;

                //Navigate: /ZxingSharp.WindowsPhone;component/Scan.xaml

                ScanPage.ScanningOptions = options;
                ScanPage.ResultFoundAction = (r) => 
                {
                    result = r;
                    scanResultResetEvent.Set();
                };

                ScanPage.UseCustomOverlay = this.UseCustomOverlay;
                ScanPage.CustomOverlay = this.CustomOverlay;
                ScanPage.TopText = TopText;
                ScanPage.BottomText = BottomText;
                ScanPage.ContinuousScanning = false;

                Dispatcher.BeginInvoke(() =>
				{
					((Microsoft.Phone.Controls.PhoneApplicationFrame)Application.Current.RootVisual).Navigate(
						new Uri("/ZXingNetMobile;component/ScanPage.xaml", UriKind.Relative));
				});

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

        public override void PauseAnalysis ()
        {
            ScanPage.RequestPauseAnalysis ();
        }

        public override void ResumeAnalysis ()
        {
            ScanPage.RequestResumeAnalysis ();
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

        internal static void Log(string message, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(message, args);
        }
    }
}
