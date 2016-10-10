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

using System;
using System.Collections.Generic;

using ZXing.Common;

namespace ZXing.OneD
{
   /// <summary>
   /// This object renders an UPC-E code as a {@link BitMatrix}.
   /// @author 0979097955s@gmail.com (RX)
   /// </summary>
   public class UPCEWriter : UPCEANWriter
   {

      private const int CODE_WIDTH = 3 + // start guard
                                     (7*6) + // bars
                                     6; // end guard

      public override BitMatrix encode(String contents,
         BarcodeFormat format,
         int width,
         int height,
         IDictionary<EncodeHintType, object> hints)
      {
         if (format != BarcodeFormat.UPC_E)
         {
            throw new ArgumentException("Can only encode UPC_E, but got " + format);
         }

         return base.encode(contents, format, width, height, hints);
      }

      public override bool[] encode(String contents)
      {
         if (contents.Length != 8)
         {
            throw new ArgumentException(
               "Requested contents should be 8 digits long, but got " + contents.Length);
         }

         var checkDigit = int.Parse(contents.Substring(7, 1));
         var parities = UPCEReader.CHECK_DIGIT_ENCODINGS[checkDigit];
         var result = new bool[CODE_WIDTH];
         var pos = 0;

         pos += appendPattern(result, pos, UPCEANReader.START_END_PATTERN, true);

         for (var i = 1; i <= 6; i++)
         {
            var digit = int.Parse(contents.Substring(i, 1));
            if ((parities >> (6 - i) & 1) == 1)
            {
               digit += 10;
            }
            pos += appendPattern(result, pos, UPCEANReader.L_AND_G_PATTERNS[digit], false);
         }

         appendPattern(result, pos, UPCEANReader.END_PATTERN, false);

         return result;
      }
   }
}