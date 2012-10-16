using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using com.google.zxing;

namespace ZxingSharp.Mobile.Test
{
    [TestClass]
    public class DecodingTests
    {
        [TestMethod]
        public void DataMatrix()
        {
            var i = GetImage("datamatrix.gif");

            var r = new com.google.zxing.datamatrix.DataMatrixReader();
           
            var result = r.decode(i);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("test", StringComparison.InvariantCultureIgnoreCase), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void QrCode()
        {
            var result = Decode("qrcode.png", BarcodeFormat.QR_CODE);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("test", StringComparison.InvariantCultureIgnoreCase), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void Ean8()
        {
            var result = Decode("ean8.png", BarcodeFormat.EAN_8);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("12345670"), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void Ean13()
        {
            var result = Decode("ean13.gif", BarcodeFormat.EAN_13);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("123456789012"), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void Code128()
        {
            var result = Decode("code128.png", BarcodeFormat.CODE_128);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("1234567"), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void Code39()
        {
            var result = Decode("code39.png", BarcodeFormat.CODE_39);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("1234567"), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void ITF()
        {
            var result = Decode("itf.png", BarcodeFormat.ITF);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("1234567890123"), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void Pdf417()
        {
            var result = Decode("pdf417.png", BarcodeFormat.PDF_417);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("PDF417"), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void UpcA()
        {
            var result = Decode("upca.png", BarcodeFormat.UPC_A);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("123456789012"), "Result Text Incorrect: " + result.getText());
        }

        [TestMethod]
        public void UpcE()
        {
            var result = Decode("upce.png", BarcodeFormat.UPC_E);

            Assert.IsNotNull(result, "NULL Result");
            Assert.IsTrue(result.getText().Equals("01234565"), "Result Text Incorrect: " + result.getText());
        }

        public com.google.zxing.MultiFormatReader GetReader(BarcodeFormat format)
        {            
            
            com.google.zxing.MultiFormatReader reader = new com.google.zxing.MultiFormatReader();
            var hints = new System.Collections.Hashtable();
            hints.Add(com.google.zxing.DecodeHintType.POSSIBLE_FORMATS, format);
            var map = new java.util.HashMap();
            var bclst = new java.util.ArrayList();
            bclst.add(format);
            map.put(com.google.zxing.DecodeHintType.POSSIBLE_FORMATS, bclst);

            reader.setHints(map);

            return reader;
        }

        public com.google.zxing.BinaryBitmap GetImage(string file)
        {

            var fullName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", file);

            var bmp = new System.Drawing.Bitmap(fullName);

            var bin = new com.google.zxing.common.HybridBinarizer(new RGBLuminanceSource(bmp, bmp.Width, bmp.Height));

            var i = new com.google.zxing.BinaryBitmap(bin);

            return i;
        }

        com.google.zxing.Result Decode(string file, BarcodeFormat format)
        {
            var r = GetReader(format);

            var i = GetImage(file);

            var result = r.decode(i);

            return result;
        }
    }
}
