using System;
using System.Runtime.InteropServices;
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
    }
}