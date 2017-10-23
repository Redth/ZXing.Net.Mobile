using System;
using System.Runtime.InteropServices;
using System.Threading;
using ApxLabs.FastAndroidCamera;

namespace ZXing.Mobile
{
    public static class FastJavaArrayEx
    {
        public static void BlockCopyTo(this FastJavaByteArray self, int sourceIndex, byte[] array, int arrayIndex, int length)
        {
            unsafe
            {
                Marshal.Copy(new IntPtr(self.Raw + sourceIndex), array, arrayIndex, Math.Min(length, Math.Min(self.Count, array.Length - arrayIndex)));
            }
        }

        static readonly ThreadLocal<byte[]> _buffer = new ThreadLocal<byte[]>();
        public static void Transpose(this FastJavaByteArray self, int width, int height)
        {
            var data = _buffer.Value;
            self.Transpose(ref data, width, height);
            _buffer.Value = data;
        }

        public static void Transpose(this FastJavaByteArray self, ref byte[] buffer, int width, int height)
        {
            var length = self.Count;

            if (length < width * height)
                throw new ArgumentException($"(this.Count) {length} < {width * height} = {width} * {height} (width * height)");

            // todo: Make transpose in-place, but this is not trivial for a non-square matrix, encoded in a 1d array.
            // Currently we spend a bit of time 

            if (buffer == null || buffer.Length < length)
                buffer = new byte[length]; // ensure we have enough buffer space for the operation

            self.BlockCopyTo(0, buffer, 0, length); // this is fairly quick (~1ms per MiB)

            unsafe
            {
                // This loop is kind of slow (~20ms per MiB)
                fixed (byte* src = &buffer[0])
                {
                    for (var y = 0; y < height; y++)
                        for (var x = 0; x < width; x++)
                            self.Raw[y + x * height] = src[x + y * width];
                }
            }
        }
    }
}
