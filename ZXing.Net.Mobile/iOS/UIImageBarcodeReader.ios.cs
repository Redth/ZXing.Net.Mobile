using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using ZXing.Mobile;

namespace ZXing.Mobile
{
	public class UIImageBarcodeReader : BarcodeReader<UIImage>, IBarcodeReader
	{
		static readonly Func<UIImage, LuminanceSource> defaultCreateLuminanceSource =
		   (img) => new RGBLuminanceSourceiOS(img);

		public UIImageBarcodeReader()
		   : this(null, defaultCreateLuminanceSource, null)
		{
		}

		public UIImageBarcodeReader(Reader reader,
		   Func<UIImage, LuminanceSource> createLuminanceSource,
		   Func<LuminanceSource, Binarizer> createBinarizer
		)
		   : base(reader, createLuminanceSource ?? defaultCreateLuminanceSource, createBinarizer)
		{
		}

		public UIImageBarcodeReader(Reader reader,
		   Func<UIImage, LuminanceSource> createLuminanceSource,
		   Func<LuminanceSource, Binarizer> createBinarizer,
		   Func<byte[], int, int, RGBLuminanceSource.BitmapFormat, LuminanceSource> createRGBLuminanceSource
		)
		   : base(reader, createLuminanceSource ?? defaultCreateLuminanceSource, createBinarizer, createRGBLuminanceSource)
		{
		}
	}
}
