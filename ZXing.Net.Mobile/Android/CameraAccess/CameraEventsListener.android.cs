using System;
using Android.Hardware;
using ApxLabs.FastAndroidCamera;

namespace ZXing.UI
{
	internal class CameraEventsListener : Java.Lang.Object, INonMarshalingPreviewCallback, Camera.IAutoFocusCallback
	{
		public event EventHandler<FastJavaByteArray> OnPreviewFrameReady;

		public void OnPreviewFrame(IntPtr data, Camera camera)
		{
			using (var fastArray = new FastJavaByteArray(data))
			{
				OnPreviewFrameReady?.Invoke(this, fastArray);

				camera.AddCallbackBuffer(fastArray);
			}
		}

		public void OnAutoFocus(bool success, Camera camera)
			=> Logger.Info($"AutoFocus Succeeded? {success}");
	}
}