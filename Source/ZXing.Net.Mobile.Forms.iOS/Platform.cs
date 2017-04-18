using System;

namespace ZXing.Net.Mobile.Forms.iOS
{
    public class Platform
    {
        public Platform ()
        {
        }

        public static void Init ()
        {
            ZXing.Net.Mobile.Forms.iOS.ZXingScannerViewRenderer.Init ();
            ZXing.Net.Mobile.Forms.iOS.ZXingBarcodeImageViewRenderer.Init ();
        }
    }
}

