using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;
using Android.Runtime;
using Android.App;
using Xamarin.Forms.Platform.Android;
using Android.Views;
using System.ComponentModel;
using ZXing.UI;
using System.Threading.Tasks;

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
					var ctrl = new ZXingSurfaceView(Context);
					SetNativeControl(ctrl);
				}

				Control.OnBarcodeScanned += Control_OnBarcodeScanned;
				e.NewElement.AutoFocusHandler = async (x, y) => await AutoFocus(x, y);
			}

			base.OnElementChanged(e);
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

		volatile bool isHandlingTouch = false;

		public override bool OnTouchEvent(MotionEvent e)
		{
			if (!isHandlingTouch)
			{
				isHandlingTouch = true;

				var x = (int)e.GetX();
				var y = (int)e.GetY();

				if (Control != null)
				{
					Logger.Info($"Touch: x={x}, y={y}");
					AutoFocus(x, y).ContinueWith(t =>
					{
						isHandlingTouch = false;
					});
				}
			}

			return base.OnTouchEvent(e);
		}

		async Task AutoFocus(int x, int y)
		{
			if (x < 0 && y < 0)
				await Control?.AutoFocusAsync();
			else
				await Control?.AutoFocusAsync(x, y);
		}
	}
}
