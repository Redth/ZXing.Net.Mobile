/*
 * Copyright 2009 ZXing authors
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
 *
 * MODIFIED: June 1, 2012 by Alex Corrado: Ported to C#
 */

using System;
using com.google.zxing;

using Android.Graphics;

/**
 * This object extends LuminanceSource around an array of YUV data returned from the camera driver,
 * with the option to crop to a rectangle within the full data. This can be used to exclude
 * superfluous pixels around the perimeter and speed up decoding.
 *
 * @author dswitkin@google.com (Daniel Switkin)
 */

namespace com.google.zxing.client.android {
public sealed class YUVLuminanceSource : LuminanceSource {

  private sbyte[] yuvData;
  private int dataWidth;
  private int dataHeight;
  private int left;
  private int top;

  public YUVLuminanceSource(sbyte[] yuvData, int dataWidth, int dataHeight, int left, int top,
      int width, int height) : base (width, height) {

    if (left + width > dataWidth || top + height > dataHeight) {
      throw new ArgumentException("Crop rectangle does not fit within image data.");
    }

    this.yuvData = yuvData;
    this.dataWidth = dataWidth;
    this.dataHeight = dataHeight;
    this.left = left;
    this.top = top;
  }

  public override sbyte[] getRow(int y, sbyte[] row) {
    if (y < 0 || y >= Height) {
      throw new ArgumentException("Requested row is outside the image: " + y);
    }
    int width = Width;
    if (row == null || row.Length < width) {
      row = new sbyte[width];
    }
    int offset = (y + top) * dataWidth + left;
    sbyte[] yuv = yuvData;
    for (int x = 0; x < width; x++) {
      row[x] = yuv[offset + x];
    }
    return row;
  }

  public override sbyte[] Matrix {
	get {
	    int width = Width;
	    int height = Height;

	    // If the caller asks for the entire underlying image, save the copy and give them the
	    // original data. The docs specifically warn that result.length must be ignored.
	    if (width == dataWidth && height == dataHeight) {
	      return yuvData;
	    }

	    int area = width * height;
	    sbyte[] matrix = new sbyte[area];
	    sbyte[] yuv = yuvData;
	    int inputOffset = top * dataWidth + left;
	    for (int y = 0; y < height; y++) {
	      int outputOffset = y * width;
	      for (int x = 0; x < width; x++) {
	        // TODO: Compare performance with using System.arraycopy().
	        matrix[outputOffset + x] = yuv[inputOffset + x];
	      }
	      inputOffset += dataWidth;
	    }
	    return matrix;
	}
  }

  public bool isCropSupported() {
    return true;
  }

  public override LuminanceSource crop(int left, int top, int width, int height) {
    return new YUVLuminanceSource(yuvData, dataWidth, dataHeight, left, top, width, height);
  }

  /**
   * Creates a greyscale Android Bitmap from the YUV data based on the crop rectangle.
   *
   * @return An 8888 bitmap.
   */
  public Bitmap renderToBitmap() {
    int width = Width;
    int height = Height;
    int[] pixels = new int[width * height];
    sbyte[] yuv = yuvData;
    int inputOffset = top * dataWidth + left;

    for (int y = 0; y < height; y++) {
      int outputOffset = y * width;
      for (int x = 0; x < width; x++) {
        int grey = yuv[inputOffset + x] & 0xff;
        pixels[outputOffset + x] = (0xff << 24) | (grey << 16) | (grey << 8) | grey;
      }
      inputOffset += dataWidth;
    }

    Bitmap bitmap = Bitmap.CreateBitmap (width, height, Bitmap.Config.Argb8888);
    bitmap.SetPixels (pixels, 0, width, 0, 0, width, height);
    return bitmap;
  }

}
}
