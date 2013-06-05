using System;
using NUnit;
using NUnit.Framework;
using ZXing;
using ZXing.Common;
using System.Drawing;
using MonoTouch.UIKit;

namespace Tests.MonoTouch
{
	public class DecodingTests
	{

		[Test]
		public void Decode_QRCode()
		{
			AssertDecode ("qrcode.png", "http://google.com");
		}

		[Test]
		public void Decode_Aztec()
		{
			AssertDecode ("aztec.png", "This is Aztec Code");
		}

		[Test]
		public void Decode_Codabar()
		{
			AssertDecode ("codabar.png", "1234567");
		}

		[Test]
		public void Decode_Code128()
		{
			AssertDecode ("code128.png", "1234567");
		}

		[Test]
		public void Decode_Code39()
		{
			AssertDecode ("code39.png", "1234567");
		}

		[Test]
		public void Decode_Code93()
		{
			AssertDecode ("code93.png", "THIS IS CODE93");
		}

		[Test]
		public void Decode_Datamatrix()
		{
			AssertDecode ("datamatrix.png", "DATAMATRIX");
		}

		[Test]
		public void Decode_Ean13()
		{
			AssertDecode ("ean13.gif", "1234567890128");
		}

		[Test]
		public void Decode_Ean8()
		{
			AssertDecode ("ean8.png", "12345670");
		}

		[Test]
		public void Decode_Itf()
		{
			AssertDecode ("itf.png", "ITF");
		}

		[Test]
		public void Decode_PDF417()
		{
			AssertDecode ("pdf417.png", "pdf417");
		}

		[Test]
		public void Decode_UpcA()
		{
			AssertDecode ("upca.png", "123456789012");
		}

		[Test]
		public void Decode_UpcE()
		{
			AssertDecode ("upce.png", "01234565");
		}

		void AssertDecode(string img, string expectedValue)
		{
			var uimg = UIImage.FromFile("Images/" + img);


			var barcodeReader = new BarcodeReader(null, (brimg) => 			                                      {

				return new RGBLuminanceSource(uimg);

			}, null, null); //(p, w, h, f) => new RGBLuminanceSource(p, w, h, RGBLuminanceSource.BitmapFormat.Unknown));

			var r = barcodeReader.Decode(uimg);
			Assert.IsNotNull(r, "No Result Found");
			Assert.IsTrue(r.Text.Equals(expectedValue, StringComparison.InvariantCultureIgnoreCase),
			              "Actual: " + r.Text + "  Expected: " + expectedValue);
		}
	}
}

