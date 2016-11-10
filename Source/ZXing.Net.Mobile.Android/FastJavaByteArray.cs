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

using System.Runtime.InteropServices;
using Android.Runtime;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Collections;
using Java.Interop;

namespace ApxLabs.FastAndroidCamera
{
	/// <summary>
	/// A wrapper around a Java array that reads elements directly from the pointer instead of through
	/// expensive JNI calls.
	/// </summary>
	public sealed class FastJavaByteArray : Java.Lang.Object, IList<byte>
	{
		#region Constructors

		/// <summary>
		/// Creates a new FastJavaByteArray with the given number of bytes reserved.
		/// </summary>
		/// <param name="length">Number of bytes to reserve</param>
		public FastJavaByteArray(int length)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException();

			var arrayHandle = JniEnvironment.Arrays.NewByteArray(length).Handle;
			if (arrayHandle == IntPtr.Zero)
				throw new OutOfMemoryException();

			// Retain a global reference to the byte array. NewByteArray() returns a local ref, and TransferLocalRef
			// creates a new global ref to the array and deletes the local ref.
			SetHandle(arrayHandle, JniHandleOwnership.TransferLocalRef);
			Count = length;

			bool isCopy = false;
			unsafe
			{
				// Get the pointer to the byte array using the global Handle
				Raw = (byte*)JniEnvironment.Arrays.GetByteArrayElements(PeerReference, &isCopy);
			}

		}

		/// <summary>
		/// Creates a FastJavaByteArray wrapper around an existing Java/JNI byte array
		/// </summary>
		/// <param name="handle">Native Java array handle</param>
		/// <param name="readOnly">Whether to consider this byte array read-only</param>
		public FastJavaByteArray(IntPtr handle, bool readOnly=true) : base(handle, JniHandleOwnership.DoNotTransfer)
		{
			// DoNotTransfer is used to leave the incoming handle alone; that reference was created in Java, so it's
			// Java's responsibility to delete it. DoNotTransfer creates a global reference to use here in the CLR
			if (handle == IntPtr.Zero)
				throw new ArgumentNullException("handle");

			IsReadOnly = readOnly;

			Count = JNIEnv.GetArrayLength(Handle);
			bool isCopy = false;
			unsafe
			{
				// Get the pointer to the byte array using the global Handle
				Raw = (byte*)JniEnvironment.Arrays.GetByteArrayElements(PeerReference, &isCopy);
			}
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			unsafe
			{
				if (Raw != null && Handle != IntPtr.Zero) // tell Java that we're done with this array
					JniEnvironment.Arrays.ReleaseByteArrayElements(PeerReference, (sbyte*)Raw, IsReadOnly ? JniReleaseArrayElementsMode.Default : JniReleaseArrayElementsMode.Commit);

				Raw = null;
			}
			base.Dispose(disposing);
		}

		#region IList<byte> Properties

		/// <summary>
		/// Count of bytes
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this byte array is read only.
		/// </summary>
		/// <value><c>true</c> if read only; otherwise, <c>false</c>.</value>
		public bool IsReadOnly
		{
			get;
			private set;
		}

		/// <summary>
		/// Indexer
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns>Byte at the given index</returns>
		public byte this[int index]
		{
			get
			{
				if (index < 0 || index >= Count)
				{
					throw new ArgumentOutOfRangeException();
				}
				byte retval;
				unsafe
				{
					retval = Raw[index];
				}
				return retval;
			}
			set
			{
				if (IsReadOnly)
				{
					throw new NotSupportedException("This FastJavaByteArray is read-only");
				}

				if (index < 0 || index >= Count)
				{
					throw new ArgumentOutOfRangeException();
				}
				unsafe
				{
					Raw[index] = value;
				}
			}
		}

		#endregion

		#region IList<byte> Methods

		/// <summary>
		/// Adds a single byte to the list. Not supported
		/// </summary>
		/// <param name="item">byte to add</param>
		public void Add(byte item)
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		/// <summary>
		/// Not supported
		/// </summary>
		public void Clear()
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		/// <summary>
		/// Returns true if the item is found int he array
		/// </summary>
		/// <param name="item">Item to find</param>
		/// <returns>True if the item is found</returns>
		public bool Contains(byte item)
		{
			return IndexOf(item) >= 0;
		}

		/// <summary>
		/// Copies the contents of the FastJavaByteArray into a byte array
		/// </summary>
		/// <param name="array">The array to copy to.</param>
		/// <param name="arrayIndex">The zero-based index into the destination array where CopyTo should start.</param>
		public void CopyTo(byte[] array, int arrayIndex)
		{
			unsafe
			{
				Marshal.Copy(new IntPtr(Raw), array, arrayIndex, Math.Min(Count, array.Length - arrayIndex));
			}
		}

		/// <summary>
		/// Copies a block of the FastJavaByteArray into a byte array
		/// </summary>
		/// <param name="sourceIndex">The zero-based index into the source where copy should start copying from.</param>
		/// <param name="array">The array to copy to.</param>
		/// <param name="arrayIndex">The zero-based index into the destination array where copy should start copying to.</param>
		/// <param name="length">The length of the block to copy.</param>
		public void BlockCopyTo(int sourceIndex, byte[] array, int arrayIndex, int length)
		{
			unsafe
			{
				Marshal.Copy(new IntPtr(Raw+sourceIndex), array, arrayIndex, Math.Min(length, Math.Min(Count, array.Length - arrayIndex)));
			}
		}

		/// <summary>
		/// Retreives enumerator
		/// </summary>
		/// <returns>Enumerator</returns>
		[DebuggerHidden]
		public IEnumerator<byte> GetEnumerator()
		{
			return new FastJavaByteArrayEnumerator(this);
		}

		/// <summary>
		/// Retreives enumerator
		/// </summary>
		/// <returns>Enumerator</returns>
		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new FastJavaByteArrayEnumerator(this);
		}

		/// <summary>
		/// Gets the first index of the given value
		/// </summary>
		/// <param name="item">Item to search for</param>
		/// <returns>Index of found item</returns>
		public int IndexOf(byte item)
		{
			for (int i = 0; i < Count; ++i)
			{
				byte current;
				unsafe
				{
					current = Raw[i];
				}
				if (current == item)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		public void Insert(int index, byte item)
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(byte item)
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt(int index)
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Get the raw pointer to the underlying data.
		/// </summary>
		public unsafe byte* Raw { get; private set; }

		#endregion
	}
}
