using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Tizen;
using ZXing.UI;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Tizen;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]

namespace ZXing.Net.Mobile.Forms.Tizen
{
	class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.UI.ZXingMediaView>
	{
		protected ZXingScannerView formsView;
		protected ZXingMediaView zxingWindow;

		public static void Init()
		{
			// Keep linker from stripping empty method
			var temp = DateTime.Now;
		}

		protected override void Dispose(bool disposing)
		{
			zxingWindow?.Dispose();
			base.Dispose(disposing);
		}
		
		protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			base.OnElementChanged(e);
			formsView = Element;

			if (zxingWindow == null)
			{
				formsView.AutoFocusRequested += (x, y) =>
				{
					if (zxingWindow != null)
					{
						if (x < 0 && y < 0)
							zxingWindow.AutoFocusAsync();
						else
							zxingWindow.AutoFocusAsync(x, y);
					}
				};
				
				zxingWindow = new ZXing.UI.ZXingMediaView(Xamarin.Forms.Forms.NativeParent, zxingWindow.Options);
				zxingWindow.Show();
				base.SetNativeControl(zxingWindow);

				if (!formsView.IsAnalyzing)
					zxingWindow.IsAnalyzing = false;
				if (formsView.IsTorchOn)
					zxingWindow.TorchAsync(formsView.IsTorchOn);
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);
			
			if (zxingWindow != null)
			{
				switch (e.PropertyName)
				{
					case nameof(ZXingScannerView.IsTorchOn):
						zxingWindow.TorchAsync(formsView.IsTorchOn);
						break;
					case nameof(ZXingScannerView.IsAnalyzing):
						zxingWindow.IsAnalyzing = formsView.IsAnalyzing;
						break;
				}
			}
		}
	}
}
