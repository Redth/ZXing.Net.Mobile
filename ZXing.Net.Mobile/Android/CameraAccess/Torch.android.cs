using Android.Content;
using Android.Content.PM;
using Android.Hardware;

namespace ZXing.Mobile.CameraAccess
{
	public class Torch
	{
		readonly CameraController cameraController;
		readonly Context context;
		bool? hasTorch;

		public Torch(CameraController cameraController, Context context)
		{
			this.cameraController = cameraController;
			this.context = context;
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

				var p = cameraController.Camera.GetParameters();
				var supportedFlashModes = p.SupportedFlashModes;

				if ((supportedFlashModes != null)
					&& (supportedFlashModes.Contains(Camera.Parameters.FlashModeTorch)
					|| supportedFlashModes.Contains(Camera.Parameters.FlashModeOn)))
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

			var parameters = cameraController.Camera.GetParameters();
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
				cameraController.Camera.SetParameters(parameters);
				IsEnabled = state;
			}
		}
	}
}