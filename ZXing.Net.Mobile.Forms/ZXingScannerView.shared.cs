using System;
using System.Windows.Input;
using Xamarin.Forms;
using ZXing.UI;

namespace ZXing.Net.Mobile.Forms
{
	public class ZXingScannerView : View
	{
		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public event Action<int, int> AutoFocusRequested;

		public ZXingScannerView()
		{
			VerticalOptions = LayoutOptions.FillAndExpand;
			HorizontalOptions = LayoutOptions.FillAndExpand;
		}

		public void RaiseOnBarcodeScanned(Result[] results)
		{
			OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(results));
			ScanResultCommand?.Execute(results);
		}

		public void ToggleTorch()
			=> IsTorchOn = !IsTorchOn;

		public void AutoFocus()
			=> AutoFocusRequested?.Invoke(-1, -1);

		public void AutoFocus(int x, int y)
			=> AutoFocusRequested?.Invoke(x, y);

		public static readonly BindableProperty OptionsProperty =
			BindableProperty.Create(nameof(Options), typeof(BarcodeScanningOptions), typeof(ZXingScannerView), new BarcodeScanningOptions());

		public BarcodeScanningOptions Options
		{
			get => (BarcodeScanningOptions)GetValue(OptionsProperty);
			set => SetValue(OptionsProperty, value);
		}

		public static readonly BindableProperty IsTorchOnProperty =
			BindableProperty.Create(nameof(IsTorchOn), typeof(bool), typeof(ZXingScannerView), false);
		public bool IsTorchOn
		{
			get => (bool)GetValue(IsTorchOnProperty);
			set => SetValue(IsTorchOnProperty, value);
		}

		public static readonly BindableProperty HasTorchProperty =
			BindableProperty.Create(nameof(HasTorch), typeof(bool), typeof(ZXingScannerView), false);

		public bool HasTorch
			=> (bool)GetValue(HasTorchProperty);

		public static readonly BindableProperty IsAnalyzingProperty =
			BindableProperty.Create(nameof(IsAnalyzing), typeof(bool), typeof(ZXingScannerView), true);

		public bool IsAnalyzing
		{
			get => (bool)GetValue(IsAnalyzingProperty);
			set => SetValue(IsAnalyzingProperty, value);
		}

		public static readonly BindableProperty ResultProperty =
			BindableProperty.Create(nameof(Result), typeof(Result), typeof(ZXingScannerView), default(Result));
		public Result Result
		{
			get => (Result)GetValue(ResultProperty);
			set => SetValue(ResultProperty, value);
		}

		public static readonly BindableProperty ScanResultCommandProperty =
			BindableProperty.Create(nameof(ScanResultCommand), typeof(ICommand), typeof(ZXingScannerView), default(ICommand));
		public ICommand ScanResultCommand
		{
			get => (ICommand)GetValue(ScanResultCommandProperty);
			set => SetValue(ScanResultCommandProperty, value);
		}
	}
}
