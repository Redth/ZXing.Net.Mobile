using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.WindowsUniversal;
using Xamarin.Forms.Platform.UWP;
using System.ComponentModel;
using System.Reflection;
using ZXing.UI;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsUniversal
{
	//[Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXingScannerUserControl>
	{
		public static void Init()
		{
			// Cause the assembly to load
		}

		protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			if (e.OldElement != null)
			{
				e.OldElement.AutoFocusHandler = null;

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
					var ctrl = new ZXingScannerUserControl();
					SetNativeControl(ctrl);
				}

				Control.OnBarcodeScanned += Control_OnBarcodeScanned;
				e.NewElement.AutoFocusHandler = async (x, y) => await Control.AutoFocusAsync(x, y);
			}

			base.OnElementChanged(e);
		}

		private void Control_OnBarcodeScanned(object sender, BarcodeScannedEventArgs e)
			=> Element?.RaiseOnBarcodeScanned(e.Results);

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (Control == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(ZXingScannerView.IsTorchOn):
					Control.TorchAsync(Element.IsTorchOn);
					break;
				case nameof(ZXingScannerView.IsAnalyzing):
					Control.IsAnalyzing = Element.IsAnalyzing;
					break;
			}
		}

		//protected override void OnDisconnectVisualChildren()
		//{
		//	Control?.Dispose();
		//	zxingControl = null;
		//	base.OnDisconnectVisualChildren();
		//}
	}
}
