using ElmSharp;
using ZXing.Common;
using ZXing.Rendering;

namespace ZXing.Mobile
{
	public class BarcodeWriter : BarcodeWriter<EvasImage>, IBarcodeWriter
	{
		public BarcodeWriter(EvasObject nativeParent)
			=> Renderer = new BitmapRenderer(nativeParent);
	}
}
