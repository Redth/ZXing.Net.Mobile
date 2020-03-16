using System;

namespace ZXing.Net.Mobile.Forms.WindowsUniversal
{
	public static class Platform
	{
		public static void Init()
		{
			ZXing.Net.Mobile.Forms.WindowsUniversal.ZXingScannerViewRenderer.Init();
			ZXing.Net.Mobile.Forms.WindowsUniversal.ZXingBarcodeImageViewRenderer.Init();
		}
	}
}
