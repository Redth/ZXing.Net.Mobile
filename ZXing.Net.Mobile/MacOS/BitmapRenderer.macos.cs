using System;
using ZXing.Rendering;

using Foundation;
using CoreFoundation;
using CoreGraphics;
using AppKit;

using ZXing.Common;

namespace ZXing.Mobile
{
	public class BitmapRenderer : IBarcodeRenderer<NSImage>
	{

		public NSImage Render(BitMatrix matrix, BarcodeFormat format, string content)
		{
			return Render(matrix, format, content, new EncodingOptions());
		}

		public NSImage Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
		{
			var context = new CGBitmapContext(null, matrix.Width, matrix.Height, 8, 0, CGColorSpace.CreateGenericGray(), CGBitmapFlags.None);

			var black = new CGColor(0f, 0f, 0f);
			var white = new CGColor(1.0f, 1.0f, 1.0f);

			for (var x = 0; x < matrix.Width; x++)
			{
				for (var y = 0; y < matrix.Height; y++)
				{
					var (cgX, cgY) = TransformToCoreGraphicsCoords(x, y, matrix);
					context.SetFillColor(matrix[cgX, cgY] ? black : white);
					context.FillRect(new CGRect(x, y, 1, 1));
				}
			}

			var img = new NSImage(context.ToImage(), new CGSize(matrix.Width, matrix.Height));
			context.Dispose();

			return img;
		}

		static (int cgX, int cgY) TransformToCoreGraphicsCoords(int x, int y, BitMatrix matrix)
			=> (x, Math.Abs(y - matrix.Height + 1));
	}
}