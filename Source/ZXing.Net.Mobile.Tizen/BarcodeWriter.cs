using ElmSharp;
using ZXing.Common;

namespace ZXing.Mobile
{using ZXing.Rendering;
    public class BarcodeWriter : BarcodeWriter<EvasImage>, IBarcodeWriter
    {
        public BarcodeWriter(Window window)
        {
            Renderer = new BitmapRenderer(window);
        }
    }
}
