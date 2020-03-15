using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using Xamarin.Forms.Platform.iOS;
using System.ComponentModel;
using System.Reflection;
using Foundation;
using ZXing.Net.Mobile.Forms.iOS;
using UIKit;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.iOS
{
	[Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingScannerView>
	{
		// No-op to be called from app to prevent linker from stripping this out    
		public static void Init()
		{
			var _ = DateTime.Now;
		}

		protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

			if (e.OldElement != null)
			{
				//e.OldElement.AutoFocusRequested -= FormsView_AutoFocusRequested;

				// Unsubscribe from event handlers and cleanup any resources
				if (Control != null)
				{
					Control.OnBarcodeScanned -= Control_OnBarcodeScanned;
					Control.Dispose();
					SetNativeControl(null);
				}
			}

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					var ctrl = new ZXing.Mobile.ZXingScannerView();
					SetNativeControl(ctrl);
				}

				Control.OnBarcodeScanned += Control_OnBarcodeScanned;
				//e.NewElement.AutoFocusRequested += FormsView_AutoFocusRequested;
			}

			base.OnElementChanged(e);
		}

		private void Control_OnBarcodeScanned(object sender, ZXing.Mobile.BarcodeScannedEventArgs e)
			=> Element?.RaiseOnBarcodeScanned(e.Results);

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (zxingView == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(ZXingScannerView.IsTorchOn):
					zxingView.Torch(formsView.IsTorchOn);
					break;
				case nameof(ZXingScannerView.IsScanning):
					if (formsView.IsScanning)
						zxingView.StartScanning(formsView.RaiseScanResult, formsView.Options);
					else
						zxingView.Stop();
					break;
				case nameof(ZXingScannerView.IsAnalyzing):
					if (formsView.IsAnalyzing)
						zxingView.ResumeAnalysis();
					else
						zxingView.PauseAnalysis();
					break;
			}
		}

		public override void TouchesEnded(NSSet touches, UIKit.UIEvent evt)
		{
			base.TouchesEnded(touches, evt);

			zxingView?.AutoFocus();
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			// Find the best guess at current orientation
			var o = UIApplication.SharedApplication.StatusBarOrientation;
			if (ViewController != null)
				o = ViewController.InterfaceOrientation;

			// Tell the native view to rotate
			zxingView?.DidRotate(o);
		}
	}
}

