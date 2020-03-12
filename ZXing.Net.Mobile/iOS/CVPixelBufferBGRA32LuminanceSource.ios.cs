using System;
using System.Runtime.InteropServices;
using CoreVideo;
using ZXing;

namespace ZXing.Mobile
{
	public class CVPixelBufferBGRA32LuminanceSource : BaseLuminanceSource
	{
		public unsafe CVPixelBufferBGRA32LuminanceSource(byte* cvPixelByteArray, int cvPixelByteArrayLength, int width, int height)
			: base(width, height) => CalculateLuminance(cvPixelByteArray, cvPixelByteArrayLength);

		public CVPixelBufferBGRA32LuminanceSource(byte[] luminances, int width, int height) : base(luminances, width, height)
		{
		}

		unsafe void CalculateLuminance(byte* rgbRawBytes, int bytesLen)
		{
			for (int rgbIndex = 0, luminanceIndex = 0; rgbIndex < bytesLen && luminanceIndex < luminances.Length; luminanceIndex++)
			{
				// Calculate luminance cheaply, favoring green.
				var b = rgbRawBytes[rgbIndex++];
				var g = rgbRawBytes[rgbIndex++];
				var r = rgbRawBytes[rgbIndex++];
				var alpha = rgbRawBytes[rgbIndex++];
				var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
				luminances[luminanceIndex] = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
			}
		}

		protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
			=> new CVPixelBufferBGRA32LuminanceSource(newLuminances, width, height);
	}
}
