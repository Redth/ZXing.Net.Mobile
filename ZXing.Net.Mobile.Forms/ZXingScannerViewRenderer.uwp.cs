using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.WindowsUniversal;
using Xamarin.Forms.Platform.UWP;
using System.ComponentModel;
using System.Reflection;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsUniversal
{
	//[Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingScannerControl>
	{
		public static void Init()
		{
			// Cause the assembly to load
		}

		protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			if (e.OldElement != null)
			{
				e.OldElement.AutoFocusRequested -= FormsView_AutoFocusRequested;

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
					var ctrl = new ZXing.Mobile.ZXingScannerControl();
					SetNativeControl(ctrl);
				}

				Control.OnBarcodeScanned += Control_OnBarcodeScanned;
				e.NewElement.AutoFocusRequested += FormsView_AutoFocusRequested;
			}

			base.OnElementChanged(e);
		}

		private void Control_OnBarcodeScanned(object sender, ZXing.Mobile.BarcodeScannedEventArgs e)
			=> Element?.RaiseOnBarcodeScanned(e.Results);

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (Control == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(ZXingScannerView.IsTorchOn):
					Control.Torch(Element.IsTorchOn);
					break;
				case nameof(ZXingScannerView.IsAnalyzing):
					if (Element.IsAnalyzing)
						Control.ResumeAnalysis();
					else
						Control.PauseAnalysis();
					break;
			}
		}

		void FormsView_AutoFocusRequested(int x, int y)
			=> Control.AutoFocus(x, y);

		//protected override void OnDisconnectVisualChildren()
		//{
		//	Control?.Dispose();
		//	zxingControl = null;
		//	base.OnDisconnectVisualChildren();
		//}
	}
}
