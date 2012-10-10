using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using ZxingSharp.Mobile;

namespace ZxingSharp.WindowsPhone.Sample
{
    public partial class MainPage : PhoneApplicationPage
    {
        UIElement customOverlayElement = null;
        ZxingScanner scanner;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            //Create a new instance of our scanner
            scanner = new ZxingScanner();
        }

        private void buttonScanDefault_Click(object sender, RoutedEventArgs e)
        {
            //Tell our scanner to use the default overlay
            scanner.UseCustomOverlay = false;
            //We can customize the top and bottom text of our default overlay
            scanner.TopText = "Hold camera up to barcode";
            scanner.BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel";
            
            //Start scanning
            scanner.StartScanning((result) =>
            {
                //Scanning finished callback
                HandleScanResult(result);
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
                scanner.StopScanning();
            };
            this.buttonFlash.Click += (s, e2) =>
            {
                scanner.ToggleTorch();
            };

            //Set our custom overlay and enable it
            scanner.CustomOverlay = customOverlayElement;
            scanner.UseCustomOverlay = true;

            //Start Scanning
            scanner.StartScanning((result) =>
            {
                //Scanning finished callback
                HandleScanResult(result);
            });
        }

        void HandleScanResult(ZxingBarcodeResult result)
        {
            string msg = "";

            if (result != null && !string.IsNullOrEmpty(result.Value))
                msg = "Found Barcode: " + result.Value;
            else
                msg = "Scanning Canceled!";

            MessageBox.Show(msg);

            //Go back to the main page
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));

            //Don't allow to navigate back to the scanner with the back button
            NavigationService.RemoveBackEntry();
        }
    }
}