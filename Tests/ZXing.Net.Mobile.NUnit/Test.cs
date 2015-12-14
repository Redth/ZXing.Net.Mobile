using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZXing;

namespace ZxingSharp.Mobile.Test
{
	[TestFixture]
	public class DecodingTests
	{
		[Test]
		public void DataMatrix()
		{
			var result = Decode("datamatrix.png", BarcodeFormat.DATA_MATRIX, new KeyValuePair<DecodeHintType, object>[] { new KeyValuePair<DecodeHintType, object>(DecodeHintType.PURE_BARCODE, "TRUE") });

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("test", StringComparison.InvariantCultureIgnoreCase), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void QrCode()
		{
			var result = Decode("qrcode.png", BarcodeFormat.QR_CODE, new KeyValuePair<DecodeHintType, object>[] { new KeyValuePair<DecodeHintType,object>(DecodeHintType.PURE_BARCODE, "TRUE") });

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("http://google.com", StringComparison.InvariantCultureIgnoreCase), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void Ean8()
		{
			var result = Decode("ean8.png", BarcodeFormat.EAN_8);

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("12345670"), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void Ean13()
		{
			var result = Decode("ean13.gif", BarcodeFormat.EAN_13);

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("1234567890128"), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void Code128()
		{
			var result = Decode("code128.png", BarcodeFormat.CODE_128);

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("1234567"), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void Code39()
		{
			var result = Decode("code39.png", BarcodeFormat.CODE_39);

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("1234567"), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void ITF()
		{
			var result = Decode("itf.png", BarcodeFormat.ITF);

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("1234567890123"), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void Pdf417()
		{
			var result = Decode("pdf417.png", BarcodeFormat.PDF_417);

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("PDF417", StringComparison.InvariantCultureIgnoreCase), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void UpcA()
		{
			var result = Decode("upca.png", BarcodeFormat.UPC_A);

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("123456789012"), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void UpcE()
		{
			var result = Decode("upce.png", BarcodeFormat.UPC_E);

			Assert.IsNotNull(result, "NULL Result");
			Assert.IsTrue(result.Text.Equals("01234565"), "Result Text Incorrect: " + result.Text);
		}

		[Test]
		public void L2Of5Small()
		{
			var result = Decode("l2of5small.png");

			Assert.IsNotNull (result, "NULL Result");
		}

		[Test]
		public void L2Of5VerySmall()
		{
			var result = Decode("l2of5verysmall.png");

			Assert.IsNotNull (result, "NULL Result");
		}

		public MultiFormatReader GetReader(BarcodeFormat? format, KeyValuePair<DecodeHintType, object>[] additionalHints)
		{

			var reader = new MultiFormatReader();

			var hints = new Dictionary<DecodeHintType, object>();

			if (format.HasValue)
				hints.Add(DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat>() { format.Value } );


			if (additionalHints != null)
				foreach (var ah in additionalHints)
					hints.Add(ah.Key, ah.Value);


			reader.Hints = hints;

			return reader;
		}

		public BinaryBitmap GetImage(string file)
		{
			var fullName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", file);

			//Try to find it from the source code folder
			if (!System.IO.File.Exists(fullName))
				fullName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Images", file);

			var bmp = new System.Drawing.Bitmap(fullName);

			var bin = new ZXing.Common.HybridBinarizer(new RGBLuminanceSource(bmp, bmp.Width, bmp.Height));

			var i = new BinaryBitmap(bin);

			return i;
		}

		Result Decode(string file, BarcodeFormat? format = null, KeyValuePair<DecodeHintType, object>[] additionalHints = null)
		{
			var r = GetReader(format, additionalHints);

			var i = GetImage(file);

			var result = r.decode(i); // decode(i);

			return result;
		}
	}
}
