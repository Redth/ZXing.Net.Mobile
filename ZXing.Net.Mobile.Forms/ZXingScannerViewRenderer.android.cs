using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;
using Android.Runtime;
using Android.App;
using Xamarin.Forms.Platform.Android;
using Android.Views;
using System.ComponentModel;
using System.Reflection;
using Android.Widget;
using ZXing.UI;
using System.Threading.Tasks;
using System.Linq.Expressions;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
	[Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXingSurfaceView>
	{
		public ZXingScannerViewRenderer(global::Android.Content.Context context)
			: base(context)
		{
		}

		public static void Init()
		{
			// Keep linker from stripping empty method
			var temp = DateTime.Now;
		}

		protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement != null)
			{
				e.OldElement.AutoFocusRequested -= Element_AutoFocusRequested;

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
					var ctrl = new ZXingSurfaceView(Context);
					SetNativeControl(ctrl);
				}

				Control.OnBarcodeScanned += Control_OnBarcodeScanned;
				e.NewElement.AutoFocusRequested += Element_AutoFocusRequested;
			}

			base.OnElementChanged(e);
		}

		void Element_AutoFocusRequested(int x, int y)
		{
			if (Control != null)
			{
				if (x < 0 && y < 0)
					Control.AutoFocusAsync();
				else
					Control.AutoFocusAsync(x, y);
			}
		}

		void Control_OnBarcodeScanned(object sender, BarcodeScannedEventArgs e)
			=> Element?.RaiseOnBarcodeScanned(e.Results);

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (Control == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(ZXingScannerView.IsTorchOn):
					Control?.TorchAsync(Element?.IsTorchOn ?? false);
					break;
				case nameof(ZXingScannerView.IsAnalyzing):
					Control.IsAnalyzing = Element.IsAnalyzing;
					break;
			}
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			var x = e.GetX();
			var y = e.GetY();

			if (Control != null)
			{
				Control.AutoFocusAsync((int)x, (int)y);
				Logger.Info($"Touch: x={x}, y={y}");
			}
			return base.OnTouchEvent(e);
		}
	}
}

