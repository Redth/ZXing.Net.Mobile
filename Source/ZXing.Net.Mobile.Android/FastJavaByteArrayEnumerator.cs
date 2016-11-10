// <copyright company="APX Labs, Inc.">
//     Copyright (c) APX Labs, Inc. All rights reserved.
// </copyright>
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using System.Collections.Generic;

namespace ApxLabs.FastAndroidCamera
{
	internal class FastJavaByteArrayEnumerator : IEnumerator<byte>
	{
		internal FastJavaByteArrayEnumerator(FastJavaByteArray arr)
		{
			if (arr == null)
				throw new ArgumentNullException();

			_arr = arr;
			_idx = 0;
		}

		public byte Current
		{
			get
			{
				byte retval;
				unsafe {
					// get value from pointer
					retval = _arr.Raw[_idx];
				}
				return retval; 
			}
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (_idx > _arr.Count)
				return false;

			++_idx;

			return _idx < _arr.Count;
		}

		public void Reset()
		{
			_idx = 0;
		}

		#region IEnumerator implementation

		object System.Collections.IEnumerator.Current {
			get {
				byte retval;
				unsafe {
					// get value from pointer
					retval = _arr.Raw[_idx];
				}
				return retval; 
			}
		}

		#endregion

		FastJavaByteArray _arr;
		int _idx;
	}
}
