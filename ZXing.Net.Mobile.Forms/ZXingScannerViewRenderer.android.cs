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
using ZXing.Mobile;
using System.Threading.Tasks;
using System.Linq.Expressions;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
	[Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingSurfaceView>
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

		protected ZXingScannerView formsView;

		protected ZXingSurfaceView zxingSurface;
		internal Task<bool> requestPermissionsTask;

		protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			base.OnElementChanged(e);

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
					var ctrl = new ZXing.Mobile.ZXingSurfaceView(Context);
					SetNativeControl(ctrl);
				}

				Control.OnBarcodeScanned += Control_OnBarcodeScanned;
				e.NewElement.AutoFocusRequested += FormsView_AutoFocusRequested;
			}

			base.OnElementChanged(e);
		}

		private void Control_OnBarcodeScanned(object sender, ZXing.Mobile.BarcodeScannedEventArgs e)
			=> Element?.RaiseOnBarcodeScanned(e.Results);

		
				// Process requests for autofocus
				//formsView.AutoFocusRequested += (x, y) =>
				//{
				//	if (zxingSurface != null)
				//	{
				//		if (x < 0 && y < 0)
				//			zxingSurface.AutoFocus();
				//		else
				//			zxingSurface.AutoFocus(x, y);
				//	}
				//};


		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (zxingSurface == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(ZXingScannerView.IsTorchOn):
					zxingSurface.Torch(formsView.IsTorchOn);
					break;
				case nameof(ZXingScannerView.IsAnalyzing):
					if (formsView.IsAnalyzing)
						zxingSurface.ResumeAnalysis();
					else
						zxingSurface.PauseAnalysis();
					break;
			}
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			var x = e.GetX();
			var y = e.GetY();

			if (zxingSurface != null)
			{
				zxingSurface.AutoFocus((int)x, (int)y);
				System.Diagnostics.Debug.WriteLine("Touch: x={0}, y={1}", x, y);
			}
			return base.OnTouchEvent(e);
		}
	}
}

