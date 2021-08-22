using System;
using System.Drawing;

namespace ZXing.Mobile
{
	/// <summary>
	/// Representation of restricted scanning area in PERCENTAGE. 
	/// Allowed values: 0 <= value <= 1 AND startY != endY
	/// Values of startY and endY are ABSOLUTE to image that means if use values of
	/// startY:0.49 and endY:0.51 we will scan only 2% of the whole image
	/// starting at 49% and finishing at 51% of the image height.
	/// </summary>
	public class ScanningArea
	{
		public float StartX { get; }
		public float StartY { get; }
		public float EndX { get; }
		public float EndY { get; }

		ScanningArea(float startX, float startY, float endX, float endY)
		{
			//if difference between parameters is less than 1% we assume those are equal
			if (Math.Abs(startY - endY) < 0.01f)
			{
				throw new ArgumentException($"Values of {nameof(startY)} and {nameof(endY)} cannot be the same");
			}

			//if difference between parameters is less than 1% we assume those are equal
			if (Math.Abs(startX - endX) < 0.01f)
			{
				throw new ArgumentException($"Values of {nameof(startX)} and {nameof(endX)} cannot be the same");
			}

			//Reverse values instead of throwing argument exception
			if (startY > endY)
			{
				var temp = endY;
				endY = startY;
				startY = temp;
			}

			if (startX > endX)
			{
				var temp = endX;
				endX = startX;
				startX = temp;
			}

			if (startY < 0)
			{
				startY = 0;
			}

			if (endY > 1)
			{
				endY = 1;
			}

			if (startX < 0)
			{
				startX = 0;
			}

			if (endX > 1)
			{
				endX = 1;
			}

			StartY = startY;
			EndY = endY;
			StartX = startX;
			EndX = endX;
		}

		public bool IsFullFrame()
		{
			if (StartX == 0.0 && StartY == 0.0 && EndX == 1.0 && EndY == 1.0)
				return true;

			return false;
		}

		public Rectangle GetCroppedRect(Rectangle rectFullImage)
		{
			Rectangle rectCropped = new Rectangle();

			rectCropped.X = (int) Math.Floor(rectFullImage.Left * StartX);
			rectCropped.Width = (int) Math.Ceiling(rectFullImage.Right * EndX) - rectCropped.X;

			rectCropped.Y = (int) Math.Floor(rectFullImage.Top * StartY);
			rectCropped.Height = (int) Math.Ceiling(rectFullImage.Bottom * EndY) - rectCropped.Y;

			return rectCropped;
		}

		public ScanningArea RotateCounterClockwise()
		{
			var startX = StartY;
			var startY = EndX;
			var endX = EndY;
			var endY = StartX;

			if (startY > endY)
			{
				startY = 1f - startY;
				endY = 1 - endY;
			}

			if (startX > endX)
			{
				startX = 1 - startX;
				endX = 1 - endX;
			}

			return new ScanningArea(startX, startY, endX, endY);
		}


		static ScanningArea _default = new ScanningArea(0f, 0f, 1f, 1f);

		/// <summary>
		/// Returns value that represents whole image.
		/// </summary>
		public static ScanningArea Default => _default;

		public static ScanningArea From(float startX, float startY, float endX, float endY) =>
			new ScanningArea(startX, startY, endX, endY);
	}
}