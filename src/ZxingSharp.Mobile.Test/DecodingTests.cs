using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.Google.Zxing;

namespace ZxingSharp.Mobile.Test
{
    [TestClass]
    public class DecodingTests
    {
        [TestMethod]
        public void DataMatrix()
        {
            var i = GetImage("datamatrix.gif");

            var r = new Com.Google.Zxing.Datamatrix.DataMatrixReader(); 
           
            var result = r.Decode(i);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("test", StringComparison.InvariantCultureIgnoreCase), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void QrCode()
        {
            var result = Decode("qrcode.png", BarcodeFormat.QR_CODE, new KeyValuePair<DecodeHintType, object>[] { new KeyValuePair<DecodeHintType,object>(DecodeHintType.PURE_BARCODE, "TRUE") });

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("http://google.com", StringComparison.InvariantCultureIgnoreCase), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void Ean8()
        {
            var result = Decode("ean8.png", BarcodeFormat.EAN_8);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("12345670"), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void Ean13()
        {
            var result = Decode("ean13.gif", BarcodeFormat.EAN_13);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("1234567890128"), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void Code128()
        {
            var result = Decode("code128.png", BarcodeFormat.CODE_128);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("1234567"), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void Code39()
        {
            var result = Decode("code39.png", BarcodeFormat.CODE_39);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("1234567"), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void ITF()
        {
            var result = Decode("itf.png", BarcodeFormat.ITF);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("1234567890123"), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void Pdf417()
        {
            var result = Decode("pdf417.png", BarcodeFormat.PDF_417);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("PDF417"), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void UpcA()
        {
            var result = Decode("upca.png", BarcodeFormat.UPC_A);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("123456789012"), "Result Text Incorrect: " + result.GetText());
        }

        [TestMethod]
        public void UpcE()
        {
            var result = Decode("upce.png", BarcodeFormat.UPC_E);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.GetText().Equals("01234565"), "Result Text Incorrect: " + result.GetText());
        }

        public Com.Google.Zxing.MultiFormatReader GetReader(BarcodeFormat format, KeyValuePair<DecodeHintType, object>[] additionalHints)
        {

            Com.Google.Zxing.MultiFormatReader reader = new Com.Google.Zxing.MultiFormatReader();

            var hints = new System.Collections.Generic.Dictionary<Com.Google.Zxing.DecodeHintType, object>();

                   


            hints.Add(DecodeHintType.POSSIBLE_FORMATS, new List<Com.Google.Zxing.BarcodeFormat>() { format } );


            if (additionalHints != null)
                foreach (var ah in additionalHints)
                    hints.Add(ah.Key, ah.Value);
    

            reader.SetHints(hints);
            
            return reader;
        }

        public Com.Google.Zxing.BinaryBitmap GetImage(string file)
        {

            var fullName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", file);

            var bmp = new System.Drawing.Bitmap(fullName);

            var bin = new Com.Google.Zxing.Common.HybridBinarizer(new RGBLuminanceSource(bmp, bmp.Width, bmp.Height));

            var i = new Com.Google.Zxing.BinaryBitmap(bin);

            return i;
        }

        Com.Google.Zxing.Result Decode(string file, BarcodeFormat format, KeyValuePair<DecodeHintType, object>[] additionalHints = null)
        {
            var r = GetReader(format, additionalHints);

            var i = GetImage(file);

            var result = r.Decode(i); // decode(i);

            return result;
        }
    }
}
