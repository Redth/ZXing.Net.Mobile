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

using ZXing.Common;

namespace ZXing.PDF417.Internal
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>Guenther Grau (Java Core)</author>
    /// <author>creatale GmbH (christoph.schulz@creatale.de)</author>
    /// <author>Stephen Furlani (C# Port)</author>
    public class PDF417CodewordDecoder
    {
        /// <summary>
        /// The ratios table
        /// </summary>
        private static readonly float[][] RATIOS_TABLE = new float[PDF417Common.SYMBOL_TABLE.Length][PDF417Common.BARS_IN_MODULE];

        private PDF417CodewordDecoder()
        {
        }

        /// <summary>
        /// Initializes the <see cref="ZXing.PDF417.Internal.PDF417CodewordDecoder"/> class & Pre-computes the symbol ratio table.
        /// </summary>
        static PDF417CodewordDecoder()
        {
            // Pre-computes the symbol ratio table.
            for (int i = 0; i < PDF417Common.SYMBOL_TABLE.Length; i++)
            {
                int currentSymbol = PDF417Common.SYMBOL_TABLE[i];
                int currentBit = currentSymbol & 0x1;
                for (int j = 0; j < PDF417Common.BARS_IN_MODULE; j++)
                {
                    float size = 0.0f;
                    while ((currentSymbol & 0x1) == currentBit)
                    {
                        size += 1.0f;
                        currentSymbol >>= 1;
                    }
                    currentBit = currentSymbol & 0x1;
                    RATIOS_TABLE[i][PDF417Common.BARS_IN_MODULE - j - 1] = size / PDF417Common.MODULES_IN_CODEWORD;
                }
            }

        }

        /// <summary>
        /// Gets the decoded value.
        /// </summary>
        /// <returns>The decoded value.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        static int GetDecodedValue(int[] moduleBitCount)
        {
            int decodedValue = GetDecodedCodewordValue(SampleBitCounts(moduleBitCount));
            if (decodedValue == PDF417Common.INVALID_CODEWORD)
            {
                decodedValue = GetClosestDecodedValue(moduleBitCount);
            }
            return decodedValue;
        }

        /// <summary>
        /// Samples the bit counts.
        /// </summary>
        /// <returns>The bit counts.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        private static int[] SampleBitCounts(int[] moduleBitCount)
        {
            float bitCountSum = PDF417Common.GetBitCountSum(moduleBitCount);
            int[] result = new int[PDF417Common.BARS_IN_MODULE];
            int bitCountIndex = 0;
            int sumPreviousBits = 0;
            for (int i = 0; i < PDF417Common.MODULES_IN_CODEWORD; i++)
            {
                float sampleIndex = 
                    bitCountSum / (2 * PDF417Common.MODULES_IN_CODEWORD) + 
                    (i * bitCountSum) / PDF417Common.MODULES_IN_CODEWORD;
                if (sumPreviousBits + moduleBitCount[bitCountIndex] <= sampleIndex)
                {
                    sumPreviousBits += moduleBitCount[bitCountIndex];
                    bitCountIndex++;
                }
                result[bitCountIndex]++;
            }
            return result;
        }

        /// <summary>
        /// Gets the decoded codeword value.
        /// </summary>
        /// <returns>The decoded codeword value.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        private static int GetDecodedCodewordValue(int[] moduleBitCount)
        {
            int decodedValue = GetBitValue(moduleBitCount);
            return PDF417Common.GetCodeword(decodedValue) == PDF417Common.INVALID_CODEWORD ? PDF417Common.INVALID_CODEWORD : decodedValue;
        }

        /// <summary>
        /// Gets the bit value.
        /// </summary>
        /// <returns>The bit value.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        private static int GetBitValue(int[] moduleBitCount)
        {
            long result = 0;
            for (int i = 0; i < moduleBitCount.Length; i++)
            {
                for (int bit = 0; bit < moduleBitCount[i]; bit++)
                {
                    result = (result << 1) | (i % 2 == 0 ? 1 : 0);
                }
            }
            return (int)result;
        }

        /// <summary>
        /// Gets the closest decoded value.
        /// </summary>
        /// <returns>The closest decoded value.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        private static int GetClosestDecodedValue(int[] moduleBitCount)
        {
            int bitCountSum = PDF417Common.GetBitCountSum(moduleBitCount);
            float[] bitCountRatios = new float[PDF417Common.BARS_IN_MODULE];
            for (int i = 0; i < bitCountRatios.Length; i++)
            {
                bitCountRatios[i] = moduleBitCount[i] / (float)bitCountSum;
            }
            float bestMatchError = float.MaxValue;
            int bestMatch = PDF417Common.INVALID_CODEWORD;
            for (int j = 0; j < RATIOS_TABLE.Length; j++)
            {
                float error = 0.0f;
                for (int k = 0; k < PDF417Common.BARS_IN_MODULE; k++)
                {
                    float diff = RATIOS_TABLE[j][k] - bitCountRatios[k];
                    error += diff * diff;
                }
                if (error < bestMatchError)
                {
                    bestMatchError = error;
                    bestMatch = PDF417Common.SYMBOL_TABLE[j];
                }
            }
            return bestMatch;
        }
    }
}

