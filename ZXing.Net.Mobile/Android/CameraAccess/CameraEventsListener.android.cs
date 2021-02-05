using System;
using System.IO;
using System.Runtime.InteropServices;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Renderscripts;
using ApxLabs.FastAndroidCamera;

using Java.IO;
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
                //Returns a yuv420888 image
                image = reader.AcquireLatestImage();

                if (image is null) return;

                //On Pixel5, the original Yuv42088 to nv21 conversion fails
                //var yuvBytes = ImageToByteArray(image);
                var yuvBytes = Yuv420888toNv21(image);

                OnPreviewFrameReady?.Invoke(this, yuvBytes);

                //To check that the yuv conversion is correct, you can save a jpg version
                //SaveJpegBytes(yuvBytes, image.Width, image.Height);
            }
            finally
            {
                image?.Close();
            }
        }

        //Use this to save the converted yuv image to external storage
        //You may need to include Permissions.StorageWrite in PermissionsHandler.android.cs and in the manifest
        //void SaveJpegBytes(byte[] yuvBytes, int width, int height)
        //{
        //    var dcimDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;
        //    var filename = System.IO.Path.Combine(dcimDir, $"nv21-output.jpg");

        //    //Convert the nv21 bytes to a jpeg
        //    var stream = new MemoryStream();
        //    var yuvImage = new YuvImage(yuvBytes, ImageFormatType.Nv21, width, height, null);
        //    yuvImage.CompressToJpeg(new Rect(0, 0, width, height), 50, stream);

        //    System.IO.File.WriteAllBytes(filename, stream.ToArray());
        //}

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

        //Convert to NV21
        byte[] ImageToByteArray(Image image)
        {
            byte[] result;
            var yBuffer = image.GetPlanes()[0].Buffer;
            var uBuffer = image.GetPlanes()[1].Buffer;
            var vBuffer = image.GetPlanes()[2].Buffer;

            var ySize = yBuffer.Remaining();
            var uSize = uBuffer.Remaining();
            var vSize = vBuffer.Remaining();

            result = new byte[ySize + uSize + vSize];

            yBuffer.Get(result, 0, ySize);
            vBuffer.Get(result, ySize, vSize);
            uBuffer.Get(result, ySize + vSize, uSize);

            return result;
        }
    }
}
