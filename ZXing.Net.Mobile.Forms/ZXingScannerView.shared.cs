using System;
using System.Windows.Input;
using Xamarin.Forms;
using ZXing.UI;

namespace ZXing.Net.Mobile.Forms
{
	public class ZXingScannerView : View
	{
		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		internal Action<int, int> AutoFocusHandler;

		public ZXingScannerView()
			: this(null)
		{
		}

		public ZXingScannerView(BarcodeScannerSettings settings)
		{
			Settings = settings ?? new BarcodeScannerSettings();
			VerticalOptions = LayoutOptions.FillAndExpand;
			HorizontalOptions = LayoutOptions.FillAndExpand;
		}

		public BarcodeScannerSettings Settings { get; }

		public void RaiseOnBarcodeScanned(Result[] results)
		{
			OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(results));
			BarcodeScannedCommand?.Execute(results);
		}

		public void ToggleTorch()
			=> IsTorchOn = !IsTorchOn;

		public void AutoFocus()
			=> AutoFocusHandler?.Invoke(-1, -1);

		public void AutoFocus(int x, int y)
			=> AutoFocusHandler?.Invoke(x, y);

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

		public static readonly BindableProperty BarcodeScannedCommandProperty =
			BindableProperty.Create(nameof(BarcodeScannedCommand), typeof(ICommand), typeof(ZXingScannerView), default(ICommand));
		public ICommand BarcodeScannedCommand
		{
			get => (ICommand)GetValue(BarcodeScannedCommandProperty);
			set => SetValue(BarcodeScannedCommandProperty, value);
		}
	}
}
