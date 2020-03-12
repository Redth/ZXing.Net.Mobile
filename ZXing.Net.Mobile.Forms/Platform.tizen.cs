using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXing.Net.Mobile.Forms.Tizen
{
	public static class Platform
	{
		public static void Init()
		{
			ZXingBarcodeImageViewRenderer.Init();
			ZXingScannerViewRenderer.Init();
		}
	}
}
