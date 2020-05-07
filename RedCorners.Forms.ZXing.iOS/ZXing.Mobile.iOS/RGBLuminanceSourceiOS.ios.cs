using System;
using System.Runtime.InteropServices;

using UIKit;
using CoreGraphics;

namespace ZXing.Mobile
{
	public class RGBLuminanceSourceiOS : RGBLuminanceSource
	{
		public RGBLuminanceSourceiOS(UIImage d)
			: base((int)d.CGImage.Width, (int)d.CGImage.Height)
			=> CalculateLuminance(d);

		void CalculateLuminance(UIImage d)
		{
			var imageRef = d.CGImage;
			var width = (int)imageRef.Width;
			var height = (int)imageRef.Height;
			var colorSpace = CGColorSpace.CreateDeviceRGB();

			var rawData = Marshal.AllocHGlobal(height * width * 4);

			try
			{
				var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;
				var context = new CGBitmapContext(rawData, width, height, 8, 4 * width,
					colorSpace, (CGImageAlphaInfo)flags);

				context.DrawImage(new CGRect(0.0f, 0.0f, (float)width, (float)height), imageRef);
				var pixelData = new byte[height * width * 4];
				Marshal.Copy(rawData, pixelData, 0, pixelData.Length);

				CalculateLuminance(pixelData, BitmapFormat.BGRA32);
			}
			finally
			{
				Marshal.FreeHGlobal(rawData);
			}
		}
	}
}

