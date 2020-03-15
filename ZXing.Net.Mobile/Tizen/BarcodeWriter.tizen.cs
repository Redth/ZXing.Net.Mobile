using ElmSharp;
using ZXing.Common;
using ZXing.Rendering;

namespace ZXing.UI
{
	public class BarcodeWriter : BarcodeWriter<EvasImage>, IBarcodeWriter
	{
		public BarcodeWriter(EvasObject nativeParent)
			=> Renderer = new BitmapRenderer(nativeParent);
	}
}
