using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using Xamarin.Forms.Platform.iOS;
using System.ComponentModel;
using System.Reflection;
using Foundation;
using ZXing.Net.Mobile.Forms.iOS;
using UIKit;
using ZXing.UI;

[assembly: ExportRenderer(typeof(ZXing.Net.Mobile.Forms.ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.iOS
{
	[Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.UI.ZXingScannerView>
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
					var ctrl = new ZXing.UI.ZXingScannerView();
					SetNativeControl(ctrl);
				}

				Control.OnBarcodeScanned += Control_OnBarcodeScanned;
			}

			base.OnElementChanged(e);
		}

		void Control_OnBarcodeScanned(object sender, ZXing.UI.BarcodeScannedEventArgs e)
			=> Element?.RaiseOnBarcodeScanned(e.Results);

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (Control == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(ZXingScannerView.IsTorchOn):
					Control.TorchAsync(Element?.IsTorchOn ?? false);
					break;
				case nameof(ZXingScannerView.IsAnalyzing):
					Control.IsAnalyzing = Element.IsAnalyzing;
					break;
			}
		}

		public override void TouchesEnded(NSSet touches, UIKit.UIEvent evt)
		{
			base.TouchesEnded(touches, evt);

			Control?.AutoFocusAsync();
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			// Find the best guess at current orientation
			var o = UIApplication.SharedApplication.StatusBarOrientation;
			if (ViewController != null)
				o = ViewController.InterfaceOrientation;

			// Tell the native view to rotate
			Control?.DidRotate(o);
		}
	}
}

