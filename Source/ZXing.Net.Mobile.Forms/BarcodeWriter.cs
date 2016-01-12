//
//using ZXing.Rendering;
//using Xamarin.Forms;
//using System.IO;
//using ZXing.Common;
//
//namespace ZXing.Mobile
//{   
//    /// <summary>
//    /// A smart class to encode some content to a barcode image
//    /// </summary>
//    public class BarcodeWriter : ZXing.IBarcodeWriterGeneric<ImageSource>
//    {
//        ZXing.BarcodeWriterPixelData writer;
//
//        /// <summary>
//        /// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
//        /// </summary>
//        public BarcodeWriter()
//        {
//            writer = new ZXing.BarcodeWriterPixelData ();
//        }
//
//        public Writer Encoder {
//            get { return writer.Encoder; }
//            set { writer.Encoder = value; }
//        }
//
//        public BarcodeFormat Format {
//            get { return writer.Format; }
//            set { writer.Format = value; }
//        }
//
//        public EncodingOptions Options { 
//            get { return writer.Options; }
//            set { writer.Options = value; }
//        }
//
////        public IBarcodeRenderer<byte[]> Renderer {
////            get { return writer.Renderer; }
////            set { writer.Renderer = value; }
////        }
//
//        public ImageSource Write (string contents)
//        {            
//            var bm = Encode (contents);
//            return Write (bm);
//        }
//
//        public ZXing.Common.BitMatrix Encode (string contents)
//        {
//            return writer.Encode (contents);
//        }
//       
//        public ImageSource Write (ZXing.Common.BitMatrix matrix)
//        {
//            var data = writer.Write (matrix);
//
//
//            return ImageSource.FromStream (() => new MemoryStream (data.Pixels));
//        }
//    }
//}
