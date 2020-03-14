using System;

namespace ZXing.Net.Mobile.Forms.MacOS
{
	public static class Platform
	{
		public static void Init()
		{
			ZXing.Net.Mobile.Forms.MacOS.ZXingBarcodeImageViewRenderer.Init();
		}
	}
}