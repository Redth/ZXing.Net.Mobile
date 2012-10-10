using System;
#if !WINDOWS_PHONE
using System.Drawing;
#endif
using System.Collections.Generic;
using System.Collections;
#if WINDOWS_PHONE
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
#endif

namespace ZxingSharp.Mobile
{
	public class ZxingBarcodeResult
	{
		public ZxingBarcodeResult ()
		{
		}

		public string Value { get;set; }
		//public sbyte[] Raw { get;set; }
		public ZxingBarcodeFormat Format { get;set; }
		public com.google.zxing.ResultPoint[] Points { get;set; }
		public Hashtable Metadata { get;set; }

		public static ZxingBarcodeResult FromZxingResult (com.google.zxing.Result result)
		{
			var r = new ZxingBarcodeResult ();

			r.Value = result.Text;
			r.Metadata = result.ResultMetadata;
            r.Points = result.ResultPoints;

			//r.Raw = new sbyte[](result.ResultPoints.Length);
			//result.RawBytes.CopyTo(r.Raw, 0);

			switch (result.BarcodeFormat.Name) 
			{
				case "QR_CODE":
					r.Format = ZxingBarcodeFormat.QrCode;
					break;
				case "DATAMATRIX":
					r.Format = ZxingBarcodeFormat.DataMatrix;
					break;
				case "UPC_E":
					r.Format = ZxingBarcodeFormat.UpcE;
					break;
				case "UPC_A":
					r.Format = ZxingBarcodeFormat.UpcA;
					break;
				case "EAN_8":
					r.Format = ZxingBarcodeFormat.Ean8;
					break;
				case "EAN_13":
					r.Format = ZxingBarcodeFormat.Ean13;
					break;
				case "CODE_128":
					r.Format = ZxingBarcodeFormat.Code128;
					break;
				case "CODE_39":
					r.Format = ZxingBarcodeFormat.Code39;
					break;
				case "ITF":
					r.Format = ZxingBarcodeFormat.Itf;
					break;
				case "PDF417":
					r.Format = ZxingBarcodeFormat.Pdf417;
					break;
			}

			return r;
		}
	}

	[Flags]
	public enum ZxingBarcodeFormat
	{
		None = 0,
		QrCode = 1,
		DataMatrix = 2,
		UpcE = 4,
		UpcA = 8,
		Ean8 = 16,
		Ean13 = 32,
		Code128 = 64,
		Code39 = 128,
		Itf = 256,
		Pdf417 = 512,
	}
}

