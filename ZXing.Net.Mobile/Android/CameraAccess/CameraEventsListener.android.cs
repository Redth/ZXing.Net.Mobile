using System;
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
                image = reader.AcquireLatestImage();
                if (image is null) return;
                var yuvBytes = ImageToByteArray(image);
                OnPreviewFrameReady?.Invoke(this, yuvBytes);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Could not start preview session");
            }
            finally
            {
                image?.Close();
            }
        }

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
