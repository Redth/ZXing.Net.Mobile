/*
 * Copyright 2009 ZXing authors
 * Modifications copyright 2016 kasper@byolimit.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using ApxLabs.FastAndroidCamera;

using System;

namespace ZXing.Mobile
{
	/// <summary>
	/// This object extends LuminanceSource around an array of YUV data returned from the camera driver,
	/// with the option to crop to a rectangle within the full data. This can be used to exclude
	/// superfluous pixels around the perimeter and speed up decoding.
	/// It works for any pixel format where the Y channel is planar and appears first, including
	/// YCbCr_420_SP and YCbCr_422_SP.
	/// </summary>
	/// <remarks>
	/// Builds upon PlanarYUVLuminanceSource from ZXing.NET, which is a .Net port of ZXing. The original code
	/// was authored by
	/// @author dswitkin@google.com (Daniel Switkin)
	/// </remarks>
	public sealed class FastJavaByteArrayYUVLuminanceSource : BaseLuminanceSource
	{
		private readonly FastJavaByteArray _yuv;
		private readonly int _dataWidth;
		private readonly int _dataHeight;
		private readonly int _left;
		private readonly int _top;

		/// <summary>
		/// Initializes a new instance of the <see cref="PlanarYUVLuminanceSource"/> class.
		/// </summary>
		/// <param name="yuvData">The yuv data.</param>
		/// <param name="dataWidth">Width of the data.</param>
		/// <param name="dataHeight">Height of the data.</param>
		/// <param name="left">The left.</param>
		/// <param name="top">The top.</param>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		/// <param name="reverseHoriz">if set to <c>true</c> [reverse horiz].</param>
		public FastJavaByteArrayYUVLuminanceSource(FastJavaByteArray yuv,
			int dataWidth,
			int dataHeight,
			int left,
			int top,
			int width,
			int height)
			: base(width, height)
		{
			if (left < 0)
				throw new ArgumentException("Negative value", nameof(left));

			if (top < 0)
				throw new ArgumentException("Negative value", nameof(top));

			if (width < 0)
				throw new ArgumentException("Negative value", nameof(width));

			if (height < 0)
				throw new ArgumentException("Negative value", nameof(height));

			if (left + width > dataWidth || top + height > dataHeight)
			{
				throw new ArgumentException("Crop rectangle does not fit within image data.");
			}

			_yuv = yuv;
			_dataWidth = dataWidth;
			_dataHeight = dataHeight;
			_left = left;
			_top = top;
		}

		/// <summary>
		/// Fetches one row of luminance data from the underlying platform's bitmap. Values range from
		/// 0 (black) to 255 (white). Because Java does not have an unsigned byte type, callers will have
		/// to bitwise and with 0xff for each value. It is preferable for implementations of this method
		/// to only fetch this row rather than the whole image, since no 2D Readers may be installed and
		/// getMatrix() may never be called.
		/// </summary>
		/// <param name="y">The row to fetch, 0 &lt;= y &lt; Height.</param>
		/// <param name="row">An optional preallocated array. If null or too small, it will be ignored.
		/// Always use the returned object, and ignore the .length of the array.</param>
		/// <returns>
		/// An array containing the luminance data of the requested row.
		/// </returns>
		override public byte[] getRow(int y, byte[] row)
		{
			if (y < 0 || y >= Height)
				throw new ArgumentException("Requested row is outside the image: " + y, nameof(y));

			var width = Width;
			if (row == null || row.Length < width)
				row = new byte[width]; // ensure we have room for the row

			var offset = (y + _top) * _dataWidth + _left;
			_yuv.BlockCopyTo(offset, row, 0, width);
			return row;
		}

		override public byte[] Matrix
		{
			get
			{
				var width = Width;
				var height = Height;

				var area = width * height;
				var matrix = new byte[area];
				var inputOffset = _top * _dataWidth + _left;

				// If the width matches the full width of the underlying data, perform a single copy.
				if (width == _dataWidth)
				{
					_yuv.BlockCopyTo(inputOffset, matrix, 0, area);
					return matrix;
				}

				// Otherwise copy one cropped row at a time.
				for (var y = 0; y < height; y++)
				{
					var outputOffset = y * width;
					_yuv.BlockCopyTo(inputOffset, matrix, outputOffset, width);
					inputOffset += _dataWidth;
				}
				return matrix;
			}
		}

		/// <returns> Whether this subclass supports cropping.</returns>
		override public bool CropSupported
			=> true;

		/// <summary>
		/// Returns a new object with cropped image data. Implementations may keep a reference to the
		/// original data rather than a copy. Only callable if CropSupported is true.
		/// </summary>
		/// <param name="left">The left coordinate, 0 &lt;= left &lt; Width.</param>
		/// <param name="top">The top coordinate, 0 &lt;= top &lt;= Height.</param>
		/// <param name="width">The width of the rectangle to crop.</param>
		/// <param name="height">The height of the rectangle to crop.</param>
		/// <returns>
		/// A cropped version of this object.
		/// </returns>
		override public LuminanceSource crop(int left, int top, int width, int height)
			=> new FastJavaByteArrayYUVLuminanceSource(_yuv,
				_dataWidth,
				_dataHeight,
				_left + left,
				_top + top,
				width,
				height);

		// Called when rotating. 
		// todo: This partially defeats the purpose as we traffic in byte[] luminances
		protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
			=> new PlanarYUVLuminanceSource(newLuminances, width, height, 0, 0, width, height, false);
	}
}