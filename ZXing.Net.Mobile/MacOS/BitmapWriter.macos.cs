using System;
using AppKit;
using ZXing.Mobile;

namespace ZXing.Mobile
{
	public class BarcodeWriter : BarcodeWriter<NSImage>, IBarcodeWriter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
		/// </summary>
		public BarcodeWriter()
		{
			Renderer = new BitmapRenderer();
		}
	}
}