using Gdk;

namespace ZXing.Net.Mobile.GTK
{
    public class BarcodeWriter : BarcodeWriter<Pixbuf>, IBarcodeWriter
    {
        public BarcodeWriter()
        {
            Renderer = new BitmapRenderer();
        }
    }
}