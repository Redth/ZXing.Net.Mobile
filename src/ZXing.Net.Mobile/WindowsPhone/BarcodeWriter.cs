using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
    /// <summary>
    /// A smart class to encode some content to a barcode image
    /// </summary>
    public class BarcodeWriter : BarcodeWriterGeneric<System.Windows.Media.Imaging.WriteableBitmap>, IBarcodeWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
        /// </summary>
        public BarcodeWriter()
        {
            Renderer = new WriteableBitmapRenderer();
        }
    }
}
