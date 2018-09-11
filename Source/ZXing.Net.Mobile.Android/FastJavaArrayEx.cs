using System;
using System.IO;
using System.Runtime.InteropServices;
using Android.Graphics;
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

        public static byte[] ConvertToJpg(this FastJavaByteArray self, int width, int height)
        {
            byte[] javaByteArray = new byte[self.Count];
            self.CopyTo(javaByteArray, 0);

            using (YuvImage yuvImage = new YuvImage(javaByteArray, ImageFormatType.Nv21, width, height, null))
            {
                using (Rect rect = new Rect(0, 0, width, height))
                {
                    byte[] jpg = null;
                    using (var os = new MemoryStream())
                    {
                        yuvImage.CompressToJpeg(rect, 100, os);
                        jpg = os.ToArray();
                        os.Close();
                    }
                    return jpg;
                }
            }
        }
    }
}