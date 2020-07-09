using System;
using ZXing.Mobile.iOS.Helpers;

namespace ZXing.Mobile
{
	public class CVPixelBufferBGRA32LuminanceSource : BaseLuminanceSource
	{
		private readonly bool shouldRotate;

		public CVPixelBufferBGRA32LuminanceSource(
			Span<byte> cvPixelByteArray,
			bool shouldRotate,
			int originalImageWidth,
			int originalImageHeight,
			ScanningArea scanningArea)
			: base(0, 0) // this is not an mistake. We calculate those values later on
		{
			this.shouldRotate = shouldRotate;

			var destinationRect = PrepareDestinationImageRect(scanningArea, originalImageWidth, originalImageHeight);
			SetupLuminanceArray(destinationRect);

			CalculateLuminance(cvPixelByteArray, originalImageWidth, destinationRect);
		}

		protected CVPixelBufferBGRA32LuminanceSource(byte[] luminances, int width, int height) : base(luminances, width,
			height)
		{
		}

		void CalculateLuminance(Span<byte> rgbRawBytes, int originalImageWidth, Rect destinationRect)
		{
			if (shouldRotate)
			{
				CalculateLuminanceWithCroppingAndRotation(rgbRawBytes, originalImageWidth, destinationRect);
			}
			else
			{
				CalculateLuminanceWithCropping(rgbRawBytes, originalImageWidth, destinationRect);
			}
		}

		protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
			=> new CVPixelBufferBGRA32LuminanceSource(newLuminances, width, height);


		void CalculateLuminanceWithCroppingAndRotation(
			Span<byte> rgbRawBytes,
			int originalImageWidth,
			Rect destinationRect)
		{
			for (int x = 0, y = 0, destinationX = 0, destinationY = 0, rgbIndex = 0; rgbIndex < rgbRawBytes.Length; x++)
			{
				//Follow the current Y. Because image in memory is represented row after row
				//we increment Y each time we've read whole line
				if (x == originalImageWidth)
				{
					x = 0;
					++y;
				}

				//Check if the current coordinates are outside of destination ScanningArea.
				//We flip the values because of rotation
				if (destinationRect.Outside(y, x))
				{
					//Pixel in memory is represented by 4 bytes (BGRA) therefore we skip whole pixel.
					rgbIndex += 4;
					continue;
				}

				//Because of the rotation and consecutive reading row by row of original image
				//we fulfill destination image column by column.
				if (destinationY == destinationRect.Height)
				{
					destinationY = 0;
					destinationX++;
				}

				var index = destinationX + (destinationY * destinationRect.Width);
				luminances[index] = CalculateLuminance(rgbRawBytes, ref rgbIndex);

				//Because of the rotation and consecutive reading row by row of original image
				//we fulfill destination image column by column.
				destinationY++;
			}
		}

		void CalculateLuminanceWithCropping(
			Span<byte> rgbRawBytes,
			int originalImageWidth,
			Rect destinationRect)
		{
			for (int x = 0, y = 0, destinationX = 0, destinationY = 0, rgbIndex = 0; rgbIndex < rgbRawBytes.Length; x++)
			{
				if (x == originalImageWidth)
				{
					x = 0;
					++y;
				}

				//Check if the current coordinates are outside of destination ScanningArea.
				if (destinationRect.Outside(x, y))
				{
					//Pixel in memory is represented by 4 bytes (BGRA) therefore we skip whole pixel.
					rgbIndex += 4;
					continue;
				}

				//Fill data row by row
				if (destinationX == destinationRect.Width)
				{
					destinationX = 0;
					destinationY++;
				}

				var index = destinationX + (destinationY * destinationRect.Width);
				luminances[index] = CalculateLuminance(rgbRawBytes, ref rgbIndex);

				destinationX++;
			}
		}

		byte CalculateLuminance(Span<byte> rgbRawBytes, ref int rgbIndex)
		{
			// Calculate luminance cheaply, favoring green.
			var b = rgbRawBytes[rgbIndex++];
			var g = rgbRawBytes[rgbIndex++];
			var r = rgbRawBytes[rgbIndex++];
			var alpha = rgbRawBytes[rgbIndex++];
			var luminance = (byte) ((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >>
									ChannelWeight);

			return (byte) (((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
		}

		Rect PrepareDestinationImageRect(ScanningArea area, int width, int height)
		{
			int left, top, right, bottom = 0;

			if (shouldRotate)
			{
				//this ones are flipped because we are rotating destination image
				left = (int) (height * area.StartX);
				right = (int) (height * area.EndX);

				top = (int) (width * area.StartY);
				bottom = (int) (width * area.EndY);

				//Flip values because we are rotating destination image
				var temp = height;
				height = width;
				width = temp;
			}
			else
			{
				left = (int) (width * area.StartX);
				right = (int) (width * area.EndX);

				top = (int) (height * area.StartY);
				bottom = (int) (height * area.EndY);
			}

			//Internally ZXing.Net uses minimum 15 rows, so we want to match that
			const int minimalAmountOfRows = 16;
			if (bottom - top < minimalAmountOfRows)
			{
				var croppedHeight = bottom - top;
				if (croppedHeight % 2 != 0)
				{
					++croppedHeight;
				}

				var diff = croppedHeight >> 1;

				//Compensate the difference both from bottom and top
				if (bottom + diff < height)
				{
					bottom += diff;
					top -= diff;
				}
				else
				{
					//to prevent bottom coordinate to go outside of the scope
					//we move the additional pixels above.
					var rest = Math.Abs(height - bottom - diff);
					top -= diff + rest;
					bottom += rest;
				}
			}


			return new Rect(left, top, right, bottom);
		}

		void SetupLuminanceArray(Rect rect)
		{
			Width = rect.Width;
			Height = rect.Height;
			luminances = new byte[Width * Height];
		}
	}
}