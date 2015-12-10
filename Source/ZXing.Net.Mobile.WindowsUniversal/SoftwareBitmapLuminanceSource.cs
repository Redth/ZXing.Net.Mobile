using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ZXing.Mobile
{
    [ComImport]
    [Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }


    public class SoftwareBitmapLuminanceSource : BaseLuminanceSource
    {
        public SoftwareBitmapLuminanceSource (SoftwareBitmap softwareBitmap) : base (softwareBitmap.PixelWidth, softwareBitmap.PixelHeight)
        {
            CalculateLuminance(softwareBitmap);
        }

        protected SoftwareBitmapLuminanceSource(int width, int height) : base(width, height)
        {
        }

        protected SoftwareBitmapLuminanceSource(byte[] luminanceArray, int width, int height) : base(luminanceArray, width, height)
        {
        }

        protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
        {
            return new SoftwareBitmapLuminanceSource(width, height) { luminances = newLuminances };
        }

        private unsafe void CalculateLuminance(SoftwareBitmap bitmap)
        {
            // Effect is hard-coded to operate on BGRA8 format only
            if (bitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8)
            {
                // In BGRA8 format, each pixel is defined by 4 bytes
                const int BYTES_PER_PIXEL = 4;

                using (var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read))
                using (var reference = buffer.CreateReference())
                {

                    if (reference is IMemoryBufferByteAccess)
                    {


                        try
                        {
                            // Get a pointer to the pixel buffer
                            byte* data;
                            uint capacity;
                            ((IMemoryBufferByteAccess)reference).GetBuffer(out data, out capacity);

                            // Get information about the BitmapBuffer
                            var desc = buffer.GetPlaneDescription(0);
                            var luminanceIndex = 0;

                            // Iterate over all pixels
                            for (uint row = 0; row < desc.Height; row++)
                            {
                                for (uint col = 0; col < desc.Width; col++)
                                {
                                    // Index of the current pixel in the buffer (defined by the next 4 bytes, BGRA8)
                                    var currPixel = desc.StartIndex + desc.Stride * row + BYTES_PER_PIXEL * col;

                                    // Read the current pixel information into b,g,r channels (leave out alpha channel)
                                    var b = data[currPixel + 0]; // Blue
                                    var g = data[currPixel + 1]; // Green
                                    var r = data[currPixel + 2]; // Red

                                    var luminance = (byte)((RChannelWeight * r + GChannelWeight * g + BChannelWeight * b) >> ChannelWeight);
                                    var alpha = data[currPixel + 3];
                                    luminance = (byte)(((luminance * alpha) >> 8) + (255 * (255 - alpha) >> 8));
                                    luminances[luminanceIndex] = luminance;
                                    luminanceIndex++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Luminance Source Failed: {0}", ex);
                        }
                    }
                }
            }
        }
    }
}
