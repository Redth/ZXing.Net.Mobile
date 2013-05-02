// /*
//  * Copyright 2009 ZXing authors
//  *
//  * Licensed under the Apache License, Version 2.0 (the "License");
//  * you may not use this file except in compliance with the License.
//  * You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0
//  *
//  * Unless required by applicable law or agreed to in writing, software
//  * distributed under the License is distributed on an "AS IS" BASIS,
//  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  * See the License for the specific language governing permissions and
//  * limitations under the License.
//  */
using System;
using System.Linq;
using System.Collections.Generic;

using ZXing.Common;

namespace ZXing.PDF417.Internal
{
    /// <summary>
    /// A Barcode Value for the PDF417 barcode.  
    /// The scanner will iterate through the bitmatrix, 
    /// and given the different methods or iterations 
    /// will increment a given barcode value's confidence.
    /// 
    /// When done, this will return the values of highest confidence.
    /// </summary>
    /// <author>Guenther Grau (Java Core)</author>
    /// <author>Stephen Furlani (C# Port)</author>
    public sealed class BarcodeValue
    {
        private readonly IDictionary<int, int> confidence = new Dictionary<int, int>();

        /// <summary>
        /// Incremenets the Confidence for a given value
        /// </summary>
        /// <param name="value">Value.</param>
        public void AddConfidenceToValue(int barcodeValue)
        {
            if (confidence.ContainsKey(barcodeValue))
            {
                confidence[barcodeValue]++;
            } else
            {
                confidence.Add(barcodeValue, 1);
            }
        }

        /// <summary>
        /// Determines the maximum occurrence of a set value and returns all values which were set with this occurrence.
        /// </summary>
        /// <returns>an array of int, containing the values with the highest occurrence, or null, if no value was set.</returns>
        public int[] GetConfidentValues()
        {
            int max = (from pair in confidence select pair.Value).Max();
            return (from pair in confidence where pair.Value == max select pair.Key).ToArray();
        }

        /// <summary>
        /// Returns the confience value for a given barcode value
        /// </summary>
        /// <param name="barcodeValue">Barcode value.</param>
        public int ConfidenceForValue(int barcodeValue)
        {
            return confidence.ContainsKey(barcodeValue) ? confidence[barcodeValue] : 0;
        }

    }
}

