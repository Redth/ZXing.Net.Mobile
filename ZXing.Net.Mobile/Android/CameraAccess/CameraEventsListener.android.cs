using System;
using Android.Media;
using Java.Lang;
using Java.Nio;
using static Android.Media.ImageReader;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraEventsListener : Java.Lang.Object, IOnImageAvailableListener
    {

        public event EventHandler<byte[]> OnPreviewFrameReady;

        public void OnImageAvailable(ImageReader reader)
        {
            Image image = null;
            try
            {
                image = reader.AcquireLatestImage();

                if (image is null) return;

                var yuvBytes = Yuv420888toNv21(image);
                yuvBytes = rotateNV21(yuvBytes, image.Width, image.Height, 90);
                OnPreviewFrameReady?.Invoke(this, yuvBytes);

            }
            finally
            {
                image?.Close();
            }
        }


        // rotation from https://stackoverflow.com/questions/44994510/how-to-convert-rotate-raw-nv21-array-image-android-media-image-from-front-ca
        public static byte[] rotateNV21(byte[] yuv,
                                int width,
                                int height,
                                int rotation)
        {
            if (rotation == 0)
                return yuv;
            if (rotation % 90 != 0 || rotation < 0 || rotation > 270)
            {
                throw new IllegalArgumentException("0 <= rotation < 360, rotation % 90 == 0");
            }

            var output = new byte[yuv.Length];
            var frameSize = width * height;
            var swap = rotation % 180 != 0;
            var xflip = rotation % 270 != 0;
            var yflip = rotation >= 180;

            for (var j = 0; j < height; j++)
            {
                for (var i = 0; i < width; i++)
                {
                    var yIn = j * width + i;
                    var uIn = frameSize + (j >> 1) * width + (i & ~1);
                    var vIn = uIn + 1;

                    var wOut = swap ? height : width;
                    var hOut = swap ? width : height;
                    var iSwapped = swap ? j : i;
                    var jSwapped = swap ? i : j;
                    var iOut = xflip ? wOut - iSwapped - 1 : iSwapped;
                    var jOut = yflip ? hOut - jSwapped - 1 : jSwapped;

                    var yOut = jOut * wOut + iOut;
                    var uOut = frameSize + (jOut >> 1) * wOut + (iOut & ~1);
                    var vOut = uOut + 1;

                    output[yOut] = (byte)(0xff & yuv[yIn]);
                    output[uOut] = (byte)(0xff & yuv[uIn]);
                    output[vOut] = (byte)(0xff & yuv[vIn]);
                }
            }
            return output;
        }

        //https://stackoverflow.com/questions/52726002/camera2-captured-picture-conversion-from-yuv-420-888-to-nv21
        byte[] Yuv420888toNv21(Image image)
        {
            var width = image.Width;
            var height = image.Height;
            var ySize = width * height;
            var uvSize = width * height / 4;

            var nv21 = new byte[ySize + uvSize * 2];

            var yBuffer = image.GetPlanes()[0].Buffer; // Y
            var uBuffer = image.GetPlanes()[1].Buffer; // U
            var vBuffer = image.GetPlanes()[2].Buffer; // V

            var rowStride = image.GetPlanes()[0].RowStride;
            var pos = 0;

            if (rowStride == width)
            {
                // likely
                yBuffer.Get(nv21, 0, ySize);
                pos += ySize;
            }
            else
            {
                var yBufferPos = -rowStride; // not an actual position
                for (; pos < ySize; pos += width)
                {
                    yBufferPos += rowStride;
                    yBuffer.Position(yBufferPos);
                    yBuffer.Get(nv21, pos, width);
                }
            }

            rowStride = image.GetPlanes()[2].RowStride;
            var pixelStride = image.GetPlanes()[2].PixelStride;

            if (pixelStride == 2 && rowStride == width && uBuffer.Get(0) == vBuffer.Get(1))
            {
                // maybe V and U planes overlap as per NV21, which means vBuffer[1] is alias of uBuffer[0]
                var savePixel = vBuffer.Get(1);
                try
                {
                    vBuffer.Put(1, (sbyte)~savePixel);
                    if (uBuffer.Get(0) == (sbyte)~savePixel)
                    {
                        vBuffer.Put(1, savePixel);
                        vBuffer.Position(0);
                        uBuffer.Position(0);
                        vBuffer.Get(nv21, ySize, 1);
                        uBuffer.Get(nv21, ySize + 1, uBuffer.Remaining());

                        return nv21; // shortcut
                    }
                }
                catch (ReadOnlyBufferException)
                {
                    // unfortunately, we cannot check if vBuffer and uBuffer overlap
                }

                // unfortunately, the check failed. We must save U and V pixel by pixel
                vBuffer.Put(1, savePixel);
            }

            // other optimizations could check if (pixelStride == 1) or (pixelStride == 2), 
            // but performance gain would be less significant

            for (var row = 0; row < height / 2; row++)
            {
                for (var col = 0; col < width / 2; col++)
                {
                    var vuPos = col * pixelStride + row * rowStride;
                    nv21[pos++] = (byte)vBuffer.Get(vuPos);
                    nv21[pos++] = (byte)uBuffer.Get(vuPos);
                }
            }

            return nv21;
        }
    }
}
