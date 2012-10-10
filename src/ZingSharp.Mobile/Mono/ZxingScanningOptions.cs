using System;
using System.Collections;
using System.Collections.Generic;
using com.google.zxing;

namespace ZxingSharp.Mobile
{
	public class ZxingScanningOptions
	{
		public ZxingScanningOptions ()
		{
		}

		public ZxingBarcodeFormat BarcodeFormat { get;set; }

		public static ZxingScanningOptions Default
		{
			get 
			{
				return new ZxingScanningOptions() { BarcodeFormat = ZxingBarcodeFormat.None };
			}
		}

		public ArrayList GetFormats()
		{
            var barcodeTypes = new ArrayList();

			var format = this.BarcodeFormat;

			if ((format & ZxingBarcodeFormat.QrCode) == ZxingBarcodeFormat.QrCode)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.QR_CODE);
			if ((format & ZxingBarcodeFormat.DataMatrix) == ZxingBarcodeFormat.DataMatrix)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.DATAMATRIX);
			if ((format & ZxingBarcodeFormat.UpcE) == ZxingBarcodeFormat.UpcE)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.UPC_E);
			if ((format & ZxingBarcodeFormat.UpcA) == ZxingBarcodeFormat.UpcA)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.UPC_A);
			if ((format & ZxingBarcodeFormat.Ean8) == ZxingBarcodeFormat.Ean8)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.EAN_8);
			if ((format & ZxingBarcodeFormat.Ean13) == ZxingBarcodeFormat.Ean13)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.EAN_13);
			if ((format & ZxingBarcodeFormat.Code128) == ZxingBarcodeFormat.Code128)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.CODE_128);
			if ((format & ZxingBarcodeFormat.Code39) == ZxingBarcodeFormat.Code39)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.CODE_39);
			if ((format & ZxingBarcodeFormat.Itf) == ZxingBarcodeFormat.Itf)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.ITF);
			if ((format & ZxingBarcodeFormat.Pdf417) == ZxingBarcodeFormat.Pdf417)
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.PDF417);


			if (barcodeTypes.Count <= 0)
			{
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.QR_CODE);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.DATAMATRIX);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.UPC_E);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.UPC_A);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.EAN_8);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.EAN_13);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.CODE_128);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.CODE_39);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.ITF);
				barcodeTypes.Add(com.google.zxing.BarcodeFormat.PDF417);
			}

			return barcodeTypes;
		}
	}
}

