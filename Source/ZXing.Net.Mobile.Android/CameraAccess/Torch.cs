using Android.Content;
using Android.Content.PM;
using Android.Hardware;

namespace ZXing.Mobile.CameraAccess
{
    public class Torch
    {
        private readonly CameraController _cameraController;
        private readonly Context _context;
        private bool? _hasTorch;

        public Torch(CameraController cameraController, Context context)
        {
            _cameraController = cameraController;
            _context = context;
        }

        public bool IsSupported
        {
            get
            {
                if (_hasTorch.HasValue)
                    return _hasTorch.Value;

                if (!_context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFlash))
                {
                    Android.Util.Log.Info(MobileBarcodeScanner.TAG, "Flash not supported on this device");
                    return false;
                }

                if (_cameraController.Camera == null)
                {
                    Android.Util.Log.Info(MobileBarcodeScanner.TAG, "Run camera first");
                    return false;
                }

                var p = _cameraController.Camera.GetParameters();
                var supportedFlashModes = p.SupportedFlashModes;

                if ((supportedFlashModes != null)
                    && (supportedFlashModes.Contains(Camera.Parameters.FlashModeTorch)
                    || supportedFlashModes.Contains(Camera.Parameters.FlashModeOn)))
                    _hasTorch = ZXing.Net.Mobile.Android.PermissionsHandler.CheckTorchPermissions(_context, false);

                return _hasTorch != null && _hasTorch.Value;
            }
        }

        public bool IsEnabled { get; private set; }

        public void TurnOn()
        {
            Enable(true);
        }

        public void TurnOff()
        {
            Enable(false);
        }

        public void Toggle()
        {
            Enable(!IsEnabled);
        }

        private void Enable(bool state)
        {
            if (!IsSupported || IsEnabled == state)
                return;

            if (_cameraController.Camera == null)
            {
                Android.Util.Log.Info(MobileBarcodeScanner.TAG, "NULL Camera, cannot toggle torch");
                return;
            }

            var parameters = _cameraController.Camera.GetParameters();
            var supportedFlashModes = parameters.SupportedFlashModes;

            var flashMode = string.Empty;
            if (state)
            {
                if (supportedFlashModes.Contains(Camera.Parameters.FlashModeTorch))
                    flashMode = Camera.Parameters.FlashModeTorch;
                else if (supportedFlashModes.Contains(Camera.Parameters.FlashModeOn))
                    flashMode = Camera.Parameters.FlashModeOn;
            }
            else
            {
                if (supportedFlashModes != null && supportedFlashModes.Contains(Camera.Parameters.FlashModeOff))
                    flashMode = Camera.Parameters.FlashModeOff;
            }

            if (!string.IsNullOrEmpty(flashMode))
            {
                parameters.FlashMode = flashMode;
                _cameraController.Camera.SetParameters(parameters);
                IsEnabled = state;
            }
        }
    }
}