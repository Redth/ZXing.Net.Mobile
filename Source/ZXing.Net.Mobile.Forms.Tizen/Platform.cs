using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXing.Net.Mobile.Forms.Tizen
{
    public class Platform
    {
        public Platform() { }
        public static void Init()
        {
            ZXingBarcodeImageViewRenderer.Init();
            ZXingScannerViewRenderer.Init();
        }
    }
}
