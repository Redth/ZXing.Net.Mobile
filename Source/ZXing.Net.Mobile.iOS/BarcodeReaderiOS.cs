﻿/*
 * Copyright 2012 ZXing.Net authors
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

#if __UNIFIED__
using UIKit;
#else
using MonoTouch.UIKit;
#endif

namespace ZXing.Mobile
{
    /// <summary>
    /// A smart class to decode the barcode inside a bitmap object
    /// </summary>
    public class BarcodeReaderiOS : BarcodeReaderGeneric<UIImage>, IBarcodeReader, IMultipleBarcodeReader
    {
        private static readonly Func<UIImage, LuminanceSource> defaultCreateLuminanceSource =
            (img) => new RGBLuminanceSourceiOS(img);
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
        /// </summary>
        public BarcodeReaderiOS()
            : this(new MultiFormatReader(), defaultCreateLuminanceSource, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
        /// </summary>
        /// <param name="reader">Sets the reader which should be used to find and decode the barcode.
        /// If null then MultiFormatReader is used</param>
        /// <param name="createLuminanceSource">Sets the function to create a luminance source object for a bitmap.
        /// If null, an exception is thrown when Decode is called</param>
        /// <param name="createBinarizer">Sets the function to create a binarizer object for a luminance source.
        /// If null then HybridBinarizer is used</param>
        public BarcodeReaderiOS(Reader reader,
            Func<UIImage, LuminanceSource> createLuminanceSource,
            Func<LuminanceSource, Binarizer> createBinarizer
        )
            : base(reader, createLuminanceSource ?? defaultCreateLuminanceSource, createBinarizer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BarcodeReader"/> class.
        /// </summary>
        /// <param name="reader">Sets the reader which should be used to find and decode the barcode.
        /// If null then MultiFormatReader is used</param>
        /// <param name="createLuminanceSource">Sets the function to create a luminance source object for a bitmap.
        /// If null, an exception is thrown when Decode is called</param>
        /// <param name="createBinarizer">Sets the function to create a binarizer object for a luminance source.
        /// If null then HybridBinarizer is used</param>
        public BarcodeReaderiOS(Reader reader,
            Func<UIImage, LuminanceSource> createLuminanceSource,
            Func<LuminanceSource, Binarizer> createBinarizer,
            Func<byte[], int, int, RGBLuminanceSource.BitmapFormat, LuminanceSource> createRGBLuminanceSource
        )
            : base(reader, createLuminanceSource ?? defaultCreateLuminanceSource, createBinarizer, createRGBLuminanceSource)
        {
        }

        /// <summary>
        /// Decodes the specified barcode bitmap.
        /// </summary>
        /// <param name="rawRGB">raw bytes of the image in RGB order</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>
        /// the result data or null
        /// </returns>
        public Result Decode(UIImage rawRGB)
        {
            if (CreateLuminanceSource == null)
            {
                throw new InvalidOperationException("You have to declare a luminance source delegate.");
            }

            if (rawRGB == null)
                throw new ArgumentNullException("rawRGB");
            var luminanceSource = CreateLuminanceSource(rawRGB);

            return Decode(luminanceSource);
        }
    }
}
