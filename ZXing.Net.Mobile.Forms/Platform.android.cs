using System;

namespace ZXing.Net.Mobile.Forms.Android
{
	public static class Platform
	{
		public static void Init()
		{
			ZXing.Net.Mobile.Forms.Android.ZXingScannerViewRenderer.Init();
			ZXing.Net.Mobile.Forms.Android.ZXingBarcodeImageViewRenderer.Init();
		}
	}
}
