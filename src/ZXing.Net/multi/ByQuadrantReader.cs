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
*/

using System.Collections.Generic;

namespace ZXing.Multi
{
   /// <summary>
   /// This class attempts to decode a barcode from an image, not by scanning the whole image,
   /// but by scanning subsets of the image. This is important when there may be multiple barcodes in
   /// an image, and detecting a barcode may find parts of multiple barcode and fail to decode
   /// (e.g. QR Codes). Instead this scans the four quadrants of the image -- and also the center
   /// 'quadrant' to cover the case where a barcode is found in the center.
   /// </summary>
   /// <seealso cref="GenericMultipleBarcodeReader" />
   public sealed class ByQuadrantReader : Reader
   {
      private readonly Reader @delegate;

      public ByQuadrantReader(Reader @delegate)
      {
         this.@delegate = @delegate;
      }

      public Result Decode(BinaryBitmap image)
      {
         return Decode(image, null);
      }

      public Result Decode(BinaryBitmap image, IDictionary<DecodeHintType, object> hints)
      {
         int width = image.Width;
         int height = image.Height;
         int halfWidth = width/2;
         int halfHeight = height/2;

         var topLeft = image.crop(0, 0, halfWidth, halfHeight);
         var result = @delegate.Decode(topLeft, hints);
         if (result != null)
            return result;

         var topRight = image.crop(halfWidth, 0, halfWidth, halfHeight);
         result = @delegate.Decode(topRight, hints);
         if (result != null)
            return result;

         var bottomLeft = image.crop(0, halfHeight, halfWidth, halfHeight);
         result = @delegate.Decode(bottomLeft, hints);
         if (result != null)
            return result;

         var bottomRight = image.crop(halfWidth, halfHeight, halfWidth, halfHeight);
         result = @delegate.Decode(bottomRight, hints);
         if (result != null)
            return result;

         int quarterWidth = halfWidth/2;
         int quarterHeight = halfHeight/2;
         var center = image.crop(quarterWidth, quarterHeight, halfWidth, halfHeight);
         return @delegate.Decode(center, hints);
      }

      public void Reset()
      {
         @delegate.Reset();
      }
   }
}
