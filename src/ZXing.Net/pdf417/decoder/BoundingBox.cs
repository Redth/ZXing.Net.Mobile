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
    /// A Bounding Box helper class
    /// </summary>
    /// <author>Guenther Grau (Java Core)</author>
    /// <author>Stephen Furlani (C# Port)</author>
    public sealed class BoundingBox
    {
        private BitMatrix Image { get; set; }

        public ResultPoint TopLeft { get; private set; }
        public ResultPoint TopRight { get; private set; }
        public ResultPoint BottomLeft { get; private set; }
        public ResultPoint BottomRight { get; private set; }

        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZXing.PDF417.Internal.BoundingBox"/> class.
        /// Will throw an exception if the corner points don't match up correctly
        /// </summary>
        /// <param name="image">Image.</param>
        /// <param name="topLeft">Top left.</param>
        /// <param name="topRight">Top right.</param>
        /// <param name="bottomLeft">Bottom left.</param>
        /// <param name="bottomRight">Bottom right.</param>
        public BoundingBox(BitMatrix image, 
                           ResultPoint topLeft, 
                           ResultPoint bottomLeft,
                           ResultPoint topRight, 
                           ResultPoint bottomRight)
        {
            if ((topLeft == null && topRight == null) ||
                (bottomLeft == null && bottomRight == null) ||
                (topLeft != null && bottomLeft == null) ||
                (topRight != null && bottomRight == null))
            {
                throw ReaderException.Instance;
            }

            this.Image = image;
            this.TopLeft = topLeft;
            this.TopRight = topRight;
            this.BottomLeft = bottomLeft;
            this.BottomRight = bottomRight;
            CalculateMinMaxValues();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZXing.PDF417.Internal.BoundingBox"/> class.
        /// </summary>
        /// <param name="box">Box.</param>
        public BoundingBox(BoundingBox box) : this (box.Image, box.TopLeft,box.BottomLeft, box.TopRight, box.BottomRight)
        {

        }

        /// <summary>
        /// Calculates the minimum and maximum X & Y values based on the corner points.
        /// </summary>
        private void CalculateMinMaxValues()
        {
            // Constructor ensures that either Left or Right is not null
            if (TopLeft == null)
            {
                TopLeft = new ResultPoint(0, TopRight.Y);
                BottomLeft = new ResultPoint(0, BottomRight.Y);
            } else if (TopRight == null)
            {
                TopRight = new ResultPoint(0, TopLeft.Y);
                BottomRight = new ResultPoint(0, TopLeft.Y);
            }

            MinX = Math.Min(TopLeft.X, BottomLeft.X);
            MinY = Math.Min(TopLeft.Y, TopRight.Y);

            MaxX = Math.Max(TopRight.X, BottomRight.X);
            MaxY = Math.Max(BottomLeft.Y, BottomRight.Y); // Y points down

        }

        /// <summary>
        /// If we adjust the width, set a new right corner coordinate and recalculate
        /// </summary>
        /// <param name="topRight">Top right.</param>
        internal void SetTopRight(ResultPoint topRight)
        {
            this.TopRight = topRight;
            CalculateMinMaxValues();
        }

        /// <summary>
        /// If we adjust the width, set a new right corner coordinate and recalculate
        /// </summary>
        /// <param name="bottomRight">Bottom right.</param>
        internal void SetBottomRight(ResultPoint bottomRight)
        {
            this.BottomRight = bottomRight;
            CalculateMinMaxValues();
        }

        /// <summary>
        /// Merge two Bounding Boxes, getting the left corners of left, and the right corners of right
        /// (Images should be the same)
        /// </summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        internal static BoundingBox Merge(BoundingBox left, BoundingBox right)
        {
            if (left == null)
                return right;
            if (right == null)
                return left;
            return new BoundingBox(left.Image, left.TopLeft, left.BottomLeft, right.TopRight, right.BottomRight);
        }


    }
}

