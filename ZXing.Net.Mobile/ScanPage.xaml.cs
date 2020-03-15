using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ZXing.Mobile;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ZXing.Mobile
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ScanPage : Page, IScannerView
	{
		public static ScanPageNavigationParameters Parameters { get; set; }


		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		public MobileBarcodeScanningOptions ScanningOptions
		{
			get => scanner?.ScanningOptions ?? options;
			set
			{
				if (scanner != null)
					scanner.ScanningOptions = value;
				else
					options = value;
			}
		}

		MobileBarcodeScanner scanner;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public ScanPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			scannerControl.parentPage = this;
			scannerControl.OverlaySettings = Parameters.OverlaySettings.WithView<UIElement>();

			scannerControl.OnBarcodeScanned += OnBarcodeScanned;
		}

		protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			try
			{
				MobileBarcodeScanner.Log("OnNavigatingFrom, stopping camera...");
			}
			catch (Exception ex)
			{
				MobileBarcodeScanner.Log("OnNavigatingFrom Error: {0}", ex);
			}

			base.OnNavigatingFrom(e);
		}

		public bool IsTorchOn
			=> scannerControl.IsTorchOn;

		public bool IsAnalyzing => scannerControl?.IsAnalyzing ?? false;

		public bool HasTorch => scannerControl?.HasTorch ?? false;

		public void Torch(bool on)
			=> scannerControl?.Torch(on);

		public void AutoFocus()
			=> scannerControl?.AutoFocus();

		public void ToggleTorch()
		=> scannerControl?.ToggleTorch();

		public void PauseAnalysis()
			=> scannerControl?.PauseAnalysis();

		public void ResumeAnalysis()
			=> scannerControl?.ResumeAnalysis();

		public void AutoFocus(int x, int y)
			=> scannerControl?.AutoFocus(x, y);
	}

	public class ScanPageNavigationParameters
	{
		public MobileBarcodeScanner Scanner { get; set; }
		public ScannerOverlaySettings<UIElement> OverlaySettings { get; set; }
	}
}
