using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;

namespace ZXing.Mobile.CameraAccess
{
    public class Torch
    {
        readonly CameraController cameraController;
        readonly Context context;
        CameraManager cameraManager;
        bool? hasTorch;

        public Torch(CameraController cameraController, Context context)
        {
            this.cameraController = cameraController;
            this.context = context;
            cameraManager = (CameraManager)context.GetSystemService(Context.CameraService);
        }

        public bool IsSupported
        {
            get
            {
                if (hasTorch.HasValue)
                    return hasTorch.Value;

                if (!context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFlash))
                {
                    Android.Util.Log.Info(MobileBarcodeScanner.TAG, "Flash not supported on this device");
                    return false;
                }

                if (cameraController.Camera == null)
                {
                    Android.Util.Log.Info(MobileBarcodeScanner.TAG, "Run camera first");
                    return false;
                }

                var characteristics = cameraManager.GetCameraCharacteristics(cameraController.CameraId.ToString());
                var cameraHasTorch = ((Java.Lang.Boolean)characteristics.Get(CameraCharacteristics.FlashInfoAvailable)).BooleanValue();

                if (cameraHasTorch)
                    hasTorch = ZXing.Net.Mobile.Android.PermissionsHandler.IsTorchPermissionDeclared();

                return hasTorch != null && hasTorch.Value;
            }
        }

        public bool IsEnabled { get; private set; }

        public void TurnOn() => Enable(true);

        public void TurnOff() => Enable(false);

        public void Toggle() => Enable(!IsEnabled);

        private void Enable(bool state)
        {
            if (!IsSupported || IsEnabled == state)
                return;

            if (cameraController.Camera == null)
            {
                Android.Util.Log.Info(MobileBarcodeScanner.TAG, "NULL Camera, cannot toggle torch");
                return;
            }

            cameraController.EnableTorch(state);
            IsEnabled = state;
        }
    }
}
