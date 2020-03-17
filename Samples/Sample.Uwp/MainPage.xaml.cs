using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Sample.WindowsUniversal;
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
using ZXing.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sample.Uwp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		UIElement customOverlayElement = null;

		public MainPage()
		{
			InitializeComponent();

			Logger.Level = LogLevel.Info;
		}


		async void buttonScanDefault_Click(object sender, RoutedEventArgs e)
		{
			//Create a new instance of our scanner
			var scanner = new BarcodeScanner(defaultOverlaySettings: new BarcodeScannerDefaultOverlaySettings
			{
				TopText = "Hold camera up to barcode",
				BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel"
			})
			{
				RootFrame = Frame,
				Dispatcher = Dispatcher
			};

			//Start scanning
			var t = await scanner.ScanOnceAsync();
			HandleScanResult(t);
		}

		async void buttonScanContinuously_Click(object sender, RoutedEventArgs e)
		{
			//Create a new instance of our scanner
			var scanner = new BarcodeScanner(
				new BarcodeScannerSettings
				{
					DelayBetweenContinuousScans = TimeSpan.FromSeconds(3)
				},
				new BarcodeScannerDefaultOverlaySettings
				{
					TopText = "Hold camera up to barcode",
					BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel"
				})
			{
				RootFrame = Frame,
				Dispatcher = Dispatcher
			};

			//Start scanning
			await scanner.ScanContinuouslyAsync(r =>
			{
				HandleScanResult(r);
			});
		}

		async void buttonScanCustom_Click(object sender, RoutedEventArgs e)
		{
			//Get our UIElement from the MainPage.xaml (this) file 
			// to use as our custom overlay
			if (customOverlayElement == null)
			{
				customOverlayElement = this.customOverlay.Children[0];
				customOverlay.Children.RemoveAt(0);
			}

			//Create a new instance of our scanner
			var scanner = new BarcodeScanner(
				new BarcodeScannerSettings
				{
					DelayBetweenContinuousScans = TimeSpan.FromSeconds(3),
					AutoRotate = true
				},
				new BarcodeScannerCustomOverlay(customOverlay))
				{
					RootFrame = Frame,
					Dispatcher = Dispatcher
				};

			//Wireup our buttons from the custom overlay
			buttonCancel.Click += (s, e2) => scanner.CancelAsync();
			buttonFlash.Click += (s, e2) => scanner.ToggleTorchAsync();

			//Start scanning
			var r = await scanner.ScanOnceAsync();
			HandleScanResult(r);
		}

		async void HandleScanResult(ZXing.Result[] results)
		{
			var msg = "";

			if (results != null && results.Any(r => !string.IsNullOrEmpty(r.Text)))
				msg = "Found Barcodes: " + string.Join("; ", results.Select(r => r.Text));
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
