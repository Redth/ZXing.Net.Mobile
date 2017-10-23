using System;
using System.Threading.Tasks;
using Android.Hardware;
using ApxLabs.FastAndroidCamera;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraEventsListener : Java.Lang.Object, INonMarshalingPreviewCallback, Camera.IAutoFocusCallback
    {
        public event EventHandler<FastJavaByteArray> OnPreviewFrameReady;
        public event EventHandler<bool> AutoFocus;

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
        public async void OnPreviewFrame(IntPtr data, Camera camera)
        {
            try
            {
                using (var fastArray = new FastJavaByteArray(data))
                {
                    await Task.Run(() => OnPreviewFrameReady?.Invoke(this, fastArray));
                    camera.AddCallbackBuffer(fastArray);
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn(MobileBarcodeScanner.TAG, $"Exception squashed! {ex.Message}");
            }
        }
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void

        public void OnAutoFocus(bool success, Camera camera)
        {
            AutoFocus?.Invoke(this, success);
        }
    }
}