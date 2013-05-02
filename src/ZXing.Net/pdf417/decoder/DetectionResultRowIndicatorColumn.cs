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
using System.Collections.Generic;
using System.Linq;

using ZXing.Common;

namespace ZXing.PDF417.Internal
{
    /// <summary>
    /// Represents a Column in the Detection Result
    /// </summary>
    /// <author>Guenther Grau (Java Core)</author>
    /// <author>Stephen Furlani (C# Port)</author>
    public sealed class DetectionResultRowIndicatorColumn : DetectionResultColumn
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is the left indicator
        /// </summary>
        /// <value><c>true</c> if this instance is left; otherwise, <c>false</c>.</value>
        public bool IsLeft { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZXing.PDF417.Internal.DetectionResultRowIndicatorColumn"/> class.
        /// </summary>
        /// <param name="box">Box.</param>
        /// <param name="isLeft">If set to <c>true</c> is left.</param>
        public DetectionResultRowIndicatorColumn(BoundingBox box, bool isLeft) : base (box)
        {
            this.IsLeft = isLeft;
        }

        public void SetRowNumbers()
        {
            (from cw in Codewords where cw != null select cw.SetRowNumberAsRowIndicatorColumn());
            Codewords.Where( cw => cw != null).Select( cw => { cw.SetRowNumberAsRowIndicatorColumn(); });
        }

    }
}

