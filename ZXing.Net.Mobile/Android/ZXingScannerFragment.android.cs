using System;
using Android.OS;
using Android.Views;
using Android.Widget;
#if __ANDROID_29__
using AndroidX.Fragment.App;
#else
using Android.Support.V4.App;
#endif

namespace ZXing.Mobile
{
	public class ZXingScannerFragment : Fragment, IScannerView
	{
		internal ZXingScannerFragment(ZxingActivity parentActivity, ScannerOverlaySettings<Android.Views.View> overlaySettings)
		{
			ParentActivity = parentActivity;
			OverlaySettings = overlaySettings;
		}

		public ZXingScannerFragment(ScannerOverlaySettings<View> overlaySettings)
			: this(null, overlaySettings) { }

		public ZXingScannerFragment()
			: this(null, null) { }

		public ScannerOverlaySettings<Android.Views.View> OverlaySettings { get; private set; }

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();
		public MobileBarcodeScanningOptions ScanningOptions
		{
			get => ParentActivity != null ? ZxingActivity.ScanningOptions : options;
			set
			{
				if (ParentActivity != null)
					ZxingActivity.ScanningOptions = value;
				else
					options = value;
			}
		}

		FrameLayout frame;

		internal ZxingActivity ParentActivity { get; private set; }

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


				if (OverlaySettings?.CustomOverlay == null)
				{
					zxingOverlay = new ZxingOverlayView(Activity, OverlaySettings);
					
					frame.AddView(zxingOverlay, layoutParams);
				}
				else
				{
					frame.AddView(OverlaySettings.CustomOverlay, layoutParams);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Create Surface View Failed: " + ex);
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

				if (OverlaySettings?.CustomOverlay == null)
					frame.AddView(zxingOverlay, layoutParams);
				else
					frame.AddView(OverlaySettings.CustomOverlay, layoutParams);
			}
		}

		public override void OnStop()
		{
			if (scanner != null)
			{
				scanner.StopScanning();
				scanner.OnBarcodeScanned -= OnBarcodeScanned;
				frame.RemoveView(scanner);
			}

			if (OverlaySettings?.CustomOverlay == null)
				frame.RemoveView(zxingOverlay);
			else
				frame.RemoveView(OverlaySettings.CustomOverlay);

			base.OnStop();
		}

		LinearLayout.LayoutParams GetChildLayoutParams()
		{
			var layoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			layoutParams.Weight = 1;
			return layoutParams;
		}

		ZXingSurfaceView scanner;
		ZxingOverlayView zxingOverlay;

		public void Torch(bool on)
			=> scanner?.Torch(on);

		public void AutoFocus()
			=> scanner?.AutoFocus();

		public void AutoFocus(int x, int y)
			=> scanner?.AutoFocus(x, y);

		Action<Result> scanCallback;

		public void PauseAnalysis()
			=> scanner?.PauseAnalysis();

		public void ResumeAnalysis()
			=> scanner?.ResumeAnalysis();

		public void ToggleTorch()
			=> scanner?.ToggleTorch();

		public bool IsTorchOn
			=> scanner?.IsTorchOn ?? false;

		public bool IsAnalyzing
			=> scanner?.IsAnalyzing ?? false;

		public bool HasTorch
			=> scanner?.HasTorch ?? false;
	}
}
