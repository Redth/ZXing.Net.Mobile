using System;

namespace ZXing.Net.Mobile.Forms.macOS
{
    public class Platform
    {
        public Platform ()
        {
        }

        public static void Init ()
        {
            ZXing.Net.Mobile.Forms.macOS.ZXingBarcodeImageViewRenderer.Init ();
        }
    }
}

