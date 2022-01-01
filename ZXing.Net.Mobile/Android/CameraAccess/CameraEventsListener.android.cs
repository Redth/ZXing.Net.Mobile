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
			// data represents a CallbackBuffer we previously added
			// be sure to return it to the camera when we are done
			if (data != null && data != IntPtr.Zero)
			{
				var fastArray = new FastJavaByteArray(data);
				// fastArray now has ownership of data

				// OnPreviewFrameReady is responsible for returning
				// the buffer back to the camera. if we return it too
				// early, the camera could start using it before we are
				// finished with it
				if (OnPreviewFrameReady != null)
				{
					// the listener is responsible for adding data back as
					// a camera buffer and disposing the fastArray. if there
					// is more than one listener, then its impossible to know
					// which listener is responsible for freeing
					if (OnPreviewFrameReady.GetInvocationList().Length > 1)
						throw new global::System.InvalidOperationException("too many listeners registered");

					// NOTE: If there are multiple listeners registered,
					// then figuring out who is response
					OnPreviewFrameReady.Invoke(this, fastArray);
				}
				else
				{
					// there were no listeners. return the buffer
					camera.AddCallbackBuffer(fastArray);
					fastArray.Dispose();
				}

			}
		}

		public void OnAutoFocus(bool success, Camera camera)
		{
			Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus {0}", success ? "Succeeded" : "Failed");
		}
	}
}
