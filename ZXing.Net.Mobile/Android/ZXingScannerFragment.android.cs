using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Android.Widget;
#if __ANDROID_29__
using AndroidX.Fragment.App;
#else
using Android.Support.V4.App;
#endif

namespace ZXing.UI
{
	public class ZXingScannerFragment : Fragment, IScannerView
	{
		internal ZXingScannerFragment(BarcodeScanningOptions options, BarcodeScannerOverlay<Android.Views.View> overlay)
		{
			Options = options ?? new BarcodeScanningOptions();
			Overlay = overlay;
		}

		public ZXingScannerFragment(BarcodeScannerOverlay<View> overlay)
			: this(null, overlay) { }

		public ZXingScannerFragment()
			: this(null, null) { }

		public BarcodeScannerOverlay<Android.Views.View> Overlay { get; }

		public BarcodeScanningOptions Options { get; }

		FrameLayout frame;

		internal ZXingScannerActivity ParentActivity { get; private set; }

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		public override View OnCreateView(LayoutInflater layoutInflater, ViewGroup viewGroup, Bundle bundle)
		{
			frame = (FrameLayout)layoutInflater.Inflate(ZXing.Net.Mobile.Resource.Layout.zxingscannerfragmentlayout, viewGroup, false);

			var layoutParams = GetChildLayoutParams();

			try
			{
				scanner = new ZXingSurfaceView(Activity);
				scanner.OnBarcodeScanned += OnBarcodeScanned;

				frame.AddView(scanner, layoutParams);


				if (Overlay?.CustomOverlay == null)
				{
					zxingOverlay = new ZXingScannerOverlayView(Activity, Overlay);

					frame.AddView(zxingOverlay, layoutParams);
				}
				else
				{
					frame.AddView(Overlay.CustomOverlay, layoutParams);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Create Surface View Failed");
			}

			return frame;
		}

		public override void OnStart()
		{
			base.OnStart();
			// won't be 0 if OnCreateView has been called before.
			if (frame.ChildCount == 0)
			{
				var layoutParams = GetChildLayoutParams();
				// reattach scanner and overlay views.
				frame.AddView(scanner, layoutParams);

				if (Overlay?.CustomOverlay == null)
					frame.AddView(zxingOverlay, layoutParams);
				else
					frame.AddView(Overlay.CustomOverlay, layoutParams);
			}
		}

		public override void OnStop()
		{
			if (scanner != null)
			{
				scanner.OnBarcodeScanned -= OnBarcodeScanned;
				scanner.Dispose();
				frame.RemoveView(scanner);
			}

			if (Overlay?.CustomOverlay == null)
				frame.RemoveView(zxingOverlay);
			else
				frame.RemoveView(Overlay.CustomOverlay);

			base.OnStop();
		}

		LinearLayout.LayoutParams GetChildLayoutParams()
		{
			var layoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			layoutParams.Weight = 1;
			return layoutParams;
		}

		ZXingSurfaceView scanner;
		ZXingScannerOverlayView zxingOverlay;

		public Task TorchAsync(bool on)
			=> scanner?.TorchAsync(on);

		public Task AutoFocusAsync()
			=> scanner?.AutoFocusAsync();

		public Task AutoFocusAsync(int x, int y)
			=> scanner?.AutoFocusAsync(x, y);

		public Task ToggleTorchAsync()
			=> scanner?.ToggleTorchAsync();

		public bool IsTorchOn
			=> scanner?.IsTorchOn ?? false;

		public bool IsAnalyzing
		{
			get => scanner?.IsAnalyzing ?? false;
			set { if (scanner != null) scanner.IsAnalyzing = value; }
		}

		public bool HasTorch
			=> scanner?.HasTorch ?? false;
	}
}
