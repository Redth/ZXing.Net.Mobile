using System;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ZXing.Mobile;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sample.WindowsUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MobileBarcodeScanner scanner;

        public MainPage()
        {
            InitializeComponent();

            //Create a new instance of our scanner
            scanner = new MobileBarcodeScanner(Dispatcher);
            scanner.Dispatcher = Dispatcher;
        }

        private void buttonScanDefault_Click(object sender, RoutedEventArgs e)
        {
            //Tell our scanner to use the default overlay
            scanner.UseCustomOverlay = false;
            //We can customize the top and bottom text of our default overlay
            scanner.TopText = "Hold camera up to barcode";
            scanner.BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel";

            //Start scanning
            scanner.Scan().ContinueWith(t => HandleScanResult(t.Result));
        }

        private void buttonScanContinuously_Click(object sender, RoutedEventArgs e)
        {
            //Tell our scanner to use the default overlay
            scanner.UseCustomOverlay = false;
            //We can customize the top and bottom text of our default overlay
            scanner.TopText = "Hold camera up to barcode";
            scanner.BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel";

            //Start scanning
            scanner.ScanContinuously(HandleScanResult);
        }

        private void buttonScanCustom_Click(object sender, RoutedEventArgs e)
        {
            var customOverlay = new DemoCustomControl();
            customOverlay.ButtonCancelClicked += (o, args) => scanner.Cancel();
            customOverlay.ButtonToggleTorchClicked += (o, args) => scanner.ToggleTorch();

            //Set our custom overlay and enable it
            scanner.CustomOverlay = customOverlay;
            scanner.UseCustomOverlay = true;

            //Start scanning
            scanner.Scan().ContinueWith(t => HandleScanResult(t.Result));
        }

        async void HandleScanResult(ZXing.Result result)
        {
            string msg = "";

            if (result != null && !string.IsNullOrEmpty(result.Text))
                msg = "Found Barcode: " + result.Text;
            else
                msg = "Scanning Canceled!";
            
            await MessageBox(msg);
            
        }

        private void buttonGenerate_Click(object sender, RoutedEventArgs e)
        {
            //Go back to the main page
            Frame.Navigate(typeof(ImagePage));           
        }

        async Task MessageBox(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {                
                var dialog = new MessageDialog(text);
                await dialog.ShowAsync();
            });
        }

    }
}
