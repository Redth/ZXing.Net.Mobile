using ElmSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tizen.Maps;

namespace ZXing.UI
{
	public partial class BarcodeScanner
	{
		ZxingScannerWindow zxingScannerWindow;

		public Window MainWindow { get; internal set; }

		void PlatformInit()
		{
			var overlay = Overlay?.WithView<Container>();
			if (overlay != null)
				overlay.CustomOverlay = CustomOverlay;

			zxingScannerWindow = new ZxingScannerWindow(Options, overlay);
			MainWindow = zxingScannerWindow;
		}

		bool PlatformIsTorchOn => zxingScannerWindow.IsTorchOn;

		Task PlatformAutoFocusAsync()
			=> zxingScannerWindow?.AutoFocusAsync();

		Task PlatformCancelAsync()
		{
			zxingScannerWindow.Unrealize();
			return Task.CompletedTask;
		}

		bool PlatformIsAnalyzing
		{
			get => zxingScannerWindow?.IsAnalyzing ?? false;
			set { if (zxingScannerWindow != null) zxingScannerWindow.IsAnalyzing = value; }
		}

		void PlatformScan(Action<Result[]> scanHandler)
		{
			zxingScannerWindow.OnBarcodeScanned +=
				(s, e) => scanHandler(e.Results);

			zxingScannerWindow.Show();
		}

		Task PlatformToggleTorchAsync()
			=> zxingScannerWindow?.ToggleTorchAsync();

		Task PlatformTorchAsync(bool on)
			=> zxingScannerWindow?.TorchAsync(on);

		public Container CustomOverlay { get; set; }
	}
}
