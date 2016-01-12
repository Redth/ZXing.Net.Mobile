using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ZXing.Mobile;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sample.WindowsUniversal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        UIElement customOverlayElement = null;
        MobileBarcodeScanner scanner;

        public MainPage()
        {
            this.InitializeComponent();

            //Create a new instance of our scanner
            scanner = new MobileBarcodeScanner(this.Dispatcher);
            scanner.Dispatcher = this.Dispatcher;
        }

        private void buttonScanDefault_Click(object sender, RoutedEventArgs e)
        {
            //Tell our scanner to use the default overlay
            scanner.UseCustomOverlay = false;
            //We can customize the top and bottom text of our default overlay
            scanner.TopText = "Hold camera up to barcode";
            scanner.BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel";

            //Start scanning
            scanner.Scan().ContinueWith(t =>
            {
                if (t.Result != null)
                    HandleScanResult(t.Result);
            });
        }

        private void buttonScanContinuously_Click(object sender, RoutedEventArgs e)
        {
            //Tell our scanner to use the default overlay
            scanner.UseCustomOverlay = false;
            //We can customize the top and bottom text of our default overlay
            scanner.TopText = "Hold camera up to barcode";
            scanner.BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel";

            //Start scanning
            scanner.ScanContinuously(async (result) =>
            {
                var msg = "Found Barcode: " + result.Text;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await MessageBox(msg);
                });
            });
        }

        private void buttonScanCustom_Click(object sender, RoutedEventArgs e)
        {
            //Get our UIElement from the MainPage.xaml (this) file 
            // to use as our custom overlay
            if (customOverlayElement == null)
            {
                customOverlayElement = this.customOverlay.Children[0];
                this.customOverlay.Children.RemoveAt(0);
            }

            //Wireup our buttons from the custom overlay
            this.buttonCancel.Click += (s, e2) =>
            {
                scanner.Cancel();
            };
            this.buttonFlash.Click += (s, e2) =>
            {
                scanner.ToggleTorch();
            };

            //Set our custom overlay and enable it
            scanner.CustomOverlay = customOverlayElement;
            scanner.UseCustomOverlay = true;

            //Start scanning
            scanner.Scan().ContinueWith(t =>
            {
                if (t.Result != null)
                    HandleScanResult(t.Result);
            });
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
