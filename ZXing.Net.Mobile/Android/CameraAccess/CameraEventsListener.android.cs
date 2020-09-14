using System;
using Android.Hardware;
using ApxLabs.FastAndroidCamera;

namespace ZXing.Mobile.CameraAccess
{
	public class CameraEventsListener : Java.Lang.Object, INonMarshalingPreviewCallback, Camera.IAutoFocusCallback
	{
		public event EventHandler<FastJavaByteArray> OnPreviewFrameReady;

		public void OnPreviewFrame(IntPtr data, Camera camera)
		{
			if (data != null && data != IntPtr.Zero)
			{
				using (var fastArray = new FastJavaByteArray(data))
				{
					OnPreviewFrameReady?.Invoke(this, fastArray);

					camera.AddCallbackBuffer(fastArray);
				}
			}
		}

		public void OnAutoFocus(bool success, Camera camera)
		{
			Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus {0}", success ? "Succeeded" : "Failed");
		}
	}
}
