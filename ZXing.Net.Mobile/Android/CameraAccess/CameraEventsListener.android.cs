using System;
using Android.Media;
using Java.Nio;
using static Android.Media.ImageReader;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraEventsListener : Java.Lang.Object, IOnImageAvailableListener
    {
        public event EventHandler<(byte[] Nv21, int Width, int Height)> OnPreviewFrameReady;

        public CameraEventsListener()
        {
        }

        public void OnImageAvailable(ImageReader reader)
        {
            Image image = null;
            try
            {
                image = reader.AcquireLatestImage();

                if (image is null) return;
                var yuvBytes = Yuv420888toNv21(image);


                OnPreviewFrameReady?.Invoke(this, (yuvBytes, image.Width, image.Height));
            }
            finally
            {
                image?.Close();
            }
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

            var yArray = new byte[yBuffer.Limit()];
            yBuffer.Get(yArray, 0, yArray.Length);

            var uArray = new byte[uBuffer.Limit()];
            uBuffer.Get(uArray, 0, uArray.Length);

            var vArray = new byte[vBuffer.Limit()];
            vBuffer.Get(vArray, 0, vArray.Length);

            var rowStride = image.GetPlanes()[0].RowStride;
            var pos = 0;

            if (rowStride == width)
            {
                // likely
                Array.Copy(yArray, 0, nv21, 0, ySize);
                pos += ySize;
            }
            else
            {
                var yBufferPos = -rowStride; // not an actual position
                for (; pos < ySize; pos += width)
                {
                    yBufferPos += rowStride;

                    Array.Copy(yArray, yBufferPos, nv21, pos, width);
                }
            }

            rowStride = image.GetPlanes()[2].RowStride;
            var pixelStride = image.GetPlanes()[2].PixelStride;

            if (pixelStride == 2 && rowStride == width && uArray[0] == vArray[1])
            {
                // maybe V and U planes overlap as per NV21, which means vBuffer[1] is alias of uBuffer[0]
                var savePixel = vArray[1];
                try
                {
                    vArray[1] = (byte)~savePixel;
                    if (uArray[0] == (sbyte)~savePixel)
                    {
                        vArray[1] = savePixel;
                        Array.Copy(vArray, 0, nv21, ySize, 1);
                        Array.Copy(vArray, 0, nv21, ySize + 1, uArray.Length);

                        return nv21; // shortcut
                    }
                }
                catch (ReadOnlyBufferException)
                {
                    // unfortunately, we cannot check if vBuffer and uBuffer overlap
                }

                // unfortunately, the check failed. We must save U and V pixel by pixel
                vArray[1] = savePixel;
            }

            // other optimizations could check if (pixelStride == 1) or (pixelStride == 2),
            // but performance gain would be less significant
            for (var row = 0; row < height / 2; row++)
            {
                for (var col = 0; col < width / 2; col++)
                {
                    var vuPos = col * pixelStride + row * rowStride;
                    nv21[pos++] = vArray[vuPos];
                    nv21[pos++] = uArray[vuPos];
                }
            }

            return nv21;
        }
    }
}
