using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using RedCorners;
using RedCorners.Forms;
using ZXing.Mobile;
using ZXing;

namespace RedCorners.Forms.ZXing.Demo.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage
    {
        public static CameraResolution HandleCameraResolutionSelectorDelegate(List<CameraResolution> availableResolutions)
        {
            //Don't know if this will ever be null or empty
            if (availableResolutions == null || availableResolutions.Count < 1)
                return new CameraResolution() { Width = 800, Height = 600 };

            //Debugging revealed that the last element in the list
            //expresses the highest resolution. This could probably be more thorough.
            return availableResolutions[availableResolutions.Count - 1];
        }

        public MainPage()
        {
            InitializeComponent();
            scannerView.Options = new MobileBarcodeScanningOptions
            {
                TryHarder = true,
#if __IOS__
                CameraResolutionSelector = HandleCameraResolutionSelectorDelegate,
#endif
                PossibleFormats = Enum.GetValues(typeof(BarcodeFormat)) as IEnumerable<BarcodeFormat>,
                DelayBetweenAnalyzingFrames = 5,
                DelayBetweenContinuousScans = 5
            };
            scannerView.OnScanResult += ScannerView_OnScanResult;
            scannerView.IsScanning = true;

            btnCapture.Clicked += BtnCapture_Clicked;
        }

        private async void BtnCapture_Clicked(object sender, EventArgs e)
        {
            var fileName = $"{Guid.NewGuid()}.jpg";
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                fileName);
            await scannerView.CapturePhotoAsync(path);
            App.Instance.RunOnUI(() =>
            {
                img.Source = path;
            });
        }

        void ScannerView_OnScanResult(global::ZXing.Result result)
        {
            App.Instance.RunOnUI(() =>
            {
                Console.WriteLine(result?.Text);
                lblBarcode.Text = result?.Text;
            });
        }
    }
}