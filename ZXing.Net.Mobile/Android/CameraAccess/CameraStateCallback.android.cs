using System;
using Android.Hardware.Camera2;
using Android.Runtime;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraStateCallback : CameraDevice.StateCallback
    {
        public Action<CameraDevice> OnDisconnectedAction;
        public Action<CameraDevice, CameraError> OnErrorAction;
        public Action<CameraDevice> OnOpenedAction;

        public override void OnDisconnected(CameraDevice camera)
            => OnDisconnectedAction?.Invoke(camera);

        public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
            => OnErrorAction?.Invoke(camera, error);

        public override void OnOpened(CameraDevice camera)
            => OnOpenedAction?.Invoke(camera);
    }
}
