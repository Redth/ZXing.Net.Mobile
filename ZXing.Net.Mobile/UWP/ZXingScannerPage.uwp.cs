using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ZXing.UI
{
	public class ZXingScannerPage : Page, IScannerView
	{
		public static ScanPageNavigationParameters Parameters { get; set; }

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		ZXingScannerUserControl scannerControl;

		Grid rootGrid;

		public BarcodeScanningOptions Options
			=> pageParameters?.Options ?? new BarcodeScanningOptions();

		public BarcodeScannerOverlay<UIElement> Overlay
			=> pageParameters?.Overlay;

		ScanPageNavigationParameters pageParameters;

		public ZXingScannerPage()
		{
			rootGrid = new Grid { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };

			Content = rootGrid;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (e.Parameter == null)
				return;

			if (!(e.Parameter is ScanPageNavigationParameters p))
				return;

			pageParameters = p;

			scannerControl = new ZXingScannerUserControl(Options, Overlay);
			scannerControl.OnBarcodeScanned += ScannerControl_OnBarcodeScanned;
			
			pageParameters.AutoFocusHandler = () => scannerControl?.AutoFocusAsync();
			pageParameters.AutoFocusXYHandler = (x, y) => scannerControl?.AutoFocusAsync(x, y);
			pageParameters.IsAnalyzingGetHandler = () => scannerControl?.IsAnalyzing ?? false;
			pageParameters.IsAnalyzingSetHandler = a => { if (scannerControl != null) scannerControl.IsAnalyzing = a; };
			pageParameters.IsTorchOnHandler = () => scannerControl?.IsTorchOn ?? false;
			pageParameters.ToggleTorchHandler = () => scannerControl?.ToggleTorchAsync();
			pageParameters.TorchHandler = (on) => scannerControl?.TorchAsync(on);

			rootGrid.Children.Add(scannerControl);
		}

		void ScannerControl_OnBarcodeScanned(object sender, BarcodeScannedEventArgs e)
			=> pageParameters?.BarcodeScannedHandler?.Invoke(e.Results);

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			pageParameters.AutoFocusHandler = null;
			pageParameters.AutoFocusXYHandler = null;
			pageParameters.IsAnalyzingGetHandler = null;
			pageParameters.IsAnalyzingSetHandler = null;
			pageParameters.IsTorchOnHandler = null;
			pageParameters.ToggleTorchHandler = null;
			pageParameters.TorchHandler = null;

			if (scannerControl != null)
				scannerControl.OnBarcodeScanned -= ScannerControl_OnBarcodeScanned;

			try
			{
				Logger.Info("OnNavigatingFrom, stopping camera...");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "OnNavigatingFrom Error");
			}

			base.OnNavigatingFrom(e);
		}

		public bool IsTorchOn
			=> scannerControl.IsTorchOn;

		public bool IsAnalyzing
		{
			get => scannerControl?.IsAnalyzing ?? false;
			set { if (scannerControl != null) scannerControl.IsAnalyzing = value; }
		}

		public bool HasTorch => scannerControl?.HasTorch ?? false;

		public Task TorchAsync(bool on)
			=> scannerControl?.TorchAsync(on);

		public Task AutoFocusAsync()
			=> scannerControl?.AutoFocusAsync();

		public Task ToggleTorchAsync()
		=> scannerControl?.ToggleTorchAsync();

		public Task AutoFocusAsync(int x, int y)
			=> scannerControl?.AutoFocusAsync(x, y);
	}

	public class ScanPageNavigationParameters
	{
		public ScanPageNavigationParameters(BarcodeScanningOptions options, BarcodeScannerOverlay<UIElement> overlay)
		{
			Options = options ?? new BarcodeScanningOptions();
			Overlay = overlay;
		}

		public Action<Result[]> BarcodeScannedHandler { get; set; }

		public BarcodeScanningOptions Options { get; }
		public BarcodeScannerOverlay<UIElement> Overlay { get; }

		public Func<bool, Task> TorchHandler { get; set; }

		public Func<Task> ToggleTorchHandler { get; set; }

		public Func<bool> IsTorchOnHandler { get; set; }

		public Func<Task> AutoFocusHandler { get; set; }

		public Func<int, int, Task> AutoFocusXYHandler { get; set; }

		public Func<bool> IsAnalyzingGetHandler { get; set; }
		public Action<bool> IsAnalyzingSetHandler { get; set; }
	}
}
