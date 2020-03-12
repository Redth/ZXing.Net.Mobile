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
using ZXing.Mobile;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sample.Uwp
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
			InitializeComponent();

			//Create a new instance of our scanner
			scanner = new MobileBarcodeScanner(this.Dispatcher);
			scanner.RootFrame = this.Frame;
			scanner.Dispatcher = this.Dispatcher;
			scanner.OnCameraError += Scanner_OnCameraError;
			scanner.OnCameraInitialized += Scanner_OnCameraInitialized; ;
		}

		void Scanner_OnCameraInitialized()
		{
			//handle initialization
		}

		void Scanner_OnCameraError(IEnumerable<string> errors)
		{
			if (errors != null)
			{
				errors.ToList().ForEach(async e => await MessageBox(e));
			}
		}


		void buttonScanDefault_Click(object sender, RoutedEventArgs e)
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

		void buttonScanContinuously_Click(object sender, RoutedEventArgs e)
		{
			//Tell our scanner to use the default overlay
			scanner.UseCustomOverlay = false;
			//We can customize the top and bottom text of our default overlay
			scanner.TopText = "Hold camera up to barcode";
			scanner.BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel";

			//Start scanning
			scanner.ScanContinuously(new MobileBarcodeScanningOptions { DelayBetweenContinuousScans = 3000 }, async (result) =>
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
				customOverlay.Children.RemoveAt(0);
			}

			//Wireup our buttons from the custom overlay
			buttonCancel.Click += (s, e2) =>
			{
				scanner.Cancel();
			};
			buttonFlash.Click += (s, e2) =>
			{
				scanner.ToggleTorch();
			};

			//Set our custom overlay and enable it
			scanner.CustomOverlay = customOverlayElement;
			scanner.UseCustomOverlay = true;

			//Start scanning
			scanner.Scan(new MobileBarcodeScanningOptions { AutoRotate = true }).ContinueWith(t =>
			{
				if (t.Result != null)
					HandleScanResult(t.Result);
			});
		}

		async void HandleScanResult(ZXing.Result result)
		{
			var msg = "";

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
