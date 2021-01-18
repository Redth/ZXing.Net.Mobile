using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using Camera = Android.Hardware.Camera;

namespace ZXing.Mobile.CameraAccess
{
	public class CameraController
	{
		readonly Context context;
		readonly ISurfaceHolder holder;
		readonly SurfaceView surfaceView;
		readonly CameraEventsListener cameraEventListener;
		int cameraId;
		IScannerSessionHost scannerHost;

		public CameraController(SurfaceView surfaceView, CameraEventsListener cameraEventListener, IScannerSessionHost scannerHost)
		{
			context = surfaceView.Context;
			holder = surfaceView.Holder;
			this.surfaceView = surfaceView;
			this.cameraEventListener = cameraEventListener;
			this.scannerHost = scannerHost;
		}

		public Camera Camera { get; private set; }

		public CameraResolution CameraResolution { get; private set; }

		public int LastCameraDisplayOrientationDegree { get; private set; }

		public void RefreshCamera()
		{
			if (holder == null) return;

			ApplyCameraSettings();

			try
			{
				Camera.SetPreviewDisplay(holder);
				Camera.StartPreview();
			}
			catch (Exception ex)
			{
				Android.Util.Log.Debug(MobileBarcodeScanner.TAG, ex.ToString());
			}
		}

		public void SetupCamera()
		{
			if (Camera != null)
				return;

			var perf = PerformanceCounter.Start();
			OpenCamera();
			PerformanceCounter.Stop(perf, "Setup Camera took {0}ms");

			if (Camera == null) return;

			perf = PerformanceCounter.Start();
			ApplyCameraSettings();

			try
			{
				Camera.SetPreviewDisplay(holder);


				var previewParameters = Camera.GetParameters();
				var previewSize = previewParameters.PreviewSize;
				var bitsPerPixel = ImageFormat.GetBitsPerPixel(previewParameters.PreviewFormat);


				var bufferSize = (previewSize.Width * previewSize.Height * bitsPerPixel) / 8;
				const int NUM_PREVIEW_BUFFERS = 5;
				for (uint i = 0; i < NUM_PREVIEW_BUFFERS; ++i)
				{
					using (var buffer = new FastJavaByteArray(bufferSize))
						Camera.AddCallbackBuffer(buffer);
				}

				Camera.StartPreview();

				Camera.SetNonMarshalingPreviewCallback(cameraEventListener);
			}
			catch (Exception ex)
			{
				Android.Util.Log.Debug(MobileBarcodeScanner.TAG, ex.ToString());
				return;
			}
			finally
			{
				PerformanceCounter.Stop(perf, "Setup Camera Parameters took {0}ms");
			}

			// Docs suggest if Auto or Macro modes, we should invoke AutoFocus at least once
			var currentFocusMode = Camera.GetParameters().FocusMode;
			if (currentFocusMode == Camera.Parameters.FocusModeAuto
				|| currentFocusMode == Camera.Parameters.FocusModeMacro)
				AutoFocus();
		}

		public void AutoFocus()
		{
			AutoFocus(0, 0, false);
		}

		public void AutoFocus(int x, int y)
		{
			// The bounds for focus areas are actually -1000 to 1000
			// So we need to translate the touch coordinates to this scale
			var focusX = x / surfaceView.Width * 2000 - 1000;
			var focusY = y / surfaceView.Height * 2000 - 1000;

			// Call the autofocus with our coords
			AutoFocus(focusX, focusY, true);
		}

		public void ShutdownCamera()
		{
			if (Camera == null) return;

			// camera release logic takes about 0.005 sec so there is no need in async releasing
			var perf = PerformanceCounter.Start();
			try
			{
				try
				{
					Camera.StopPreview();
					Camera.SetNonMarshalingPreviewCallback(null);

					//Camera.SetPreviewCallback(null);

					Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"Calling SetPreviewDisplay: null");
					Camera.SetPreviewDisplay(null);
				}
				catch (Exception ex)
				{
					Android.Util.Log.Error(MobileBarcodeScanner.TAG, ex.ToString());
				}
				Camera.Release();
				Camera = null;
			}
			catch (Exception e)
			{
				Android.Util.Log.Error(MobileBarcodeScanner.TAG, e.ToString());
			}

			PerformanceCounter.Stop(perf, "Shutdown camera took {0}ms");
		}

		void OpenCamera()
		{
			try
			{
				var version = Build.VERSION.SdkInt;

				if (version >= BuildVersionCodes.Gingerbread)
				{
					Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Checking Number of cameras...");

					var numCameras = Camera.NumberOfCameras;
					var camInfo = new Camera.CameraInfo();
					var found = false;
					Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Found " + numCameras + " cameras...");

					var whichCamera = CameraFacing.Back;

					if (scannerHost.ScanningOptions.UseFrontCameraIfAvailable.HasValue &&
						scannerHost.ScanningOptions.UseFrontCameraIfAvailable.Value)
						whichCamera = CameraFacing.Front;

					for (var i = 0; i < numCameras; i++)
					{
						Camera.GetCameraInfo(i, camInfo);
						if (camInfo.Facing == whichCamera)
						{
							Android.Util.Log.Debug(MobileBarcodeScanner.TAG,
								"Found " + whichCamera + " Camera, opening...");
							Camera = Camera.Open(i);
							cameraId = i;
							found = true;
							break;
						}
					}

					if (!found)
					{
						Android.Util.Log.Debug(MobileBarcodeScanner.TAG,
							"Finding " + whichCamera + " camera failed, opening camera 0...");
						Camera = Camera.Open(0);
						cameraId = 0;
					}
				}
				else
				{
					Camera = Camera.Open();
				}

				//if (Camera != null)
				//    Camera.SetPreviewCallback(_cameraEventListener);
				//else
				//    MobileBarcodeScanner.LogWarn(MobileBarcodeScanner.TAG, "Camera is null :(");
			}
			catch (Exception ex)
			{
				ShutdownCamera();
				MobileBarcodeScanner.LogError("Setup Error: {0}", ex);
			}
		}

		void ApplyCameraSettings()
		{
			if (Camera == null)
			{
				OpenCamera();
			}

			// do nothing if something wrong with camera
			if (Camera == null) return;

			var parameters = Camera.GetParameters();
			parameters.PreviewFormat = ImageFormatType.Nv21;

			var supportedFocusModes = parameters.SupportedFocusModes;
			if (scannerHost.ScanningOptions.DisableAutofocus)
				parameters.FocusMode = Camera.Parameters.FocusModeFixed;
			else if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich &&
				supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture))
				parameters.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
			else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousVideo))
				parameters.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
			else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
				parameters.FocusMode = Camera.Parameters.FocusModeAuto;
			else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeFixed))
				parameters.FocusMode = Camera.Parameters.FocusModeFixed;

			var selectedFps = parameters.SupportedPreviewFpsRange.FirstOrDefault();
			if (selectedFps != null)
			{
				// This will make sure we select a range with the highest maximum fps
				// which still has the lowest minimum fps (Widest Range)
				foreach (var fpsRange in parameters.SupportedPreviewFpsRange)
				{
					if (fpsRange[1] > selectedFps[1] || fpsRange[1] == selectedFps[1] && fpsRange[0] < selectedFps[0])
						selectedFps = fpsRange;
				}
				parameters.SetPreviewFpsRange(selectedFps[0], selectedFps[1]);
			}

			CameraResolution resolution = null;
			var supportedPreviewSizes = parameters.SupportedPreviewSizes;
			if (supportedPreviewSizes != null)
			{
				var availableResolutions = supportedPreviewSizes.Select(sps => new CameraResolution
				{
					Width = sps.Width,
					Height = sps.Height
				});

				// Try and get a desired resolution from the options selector
				resolution = scannerHost.ScanningOptions.GetResolution(availableResolutions.ToList());

				// If the user did not specify a resolution, let's try and find a suitable one
				if (resolution == null)
				{
					foreach (var sps in supportedPreviewSizes)
					{
						if (sps.Width >= 640 && sps.Width <= 1000 && sps.Height >= 360 && sps.Height <= 1000)
						{
							resolution = new CameraResolution
							{
								Width = sps.Width,
								Height = sps.Height
							};
							break;
						}
					}
				}
			}

			// Google Glass requires this fix to display the camera output correctly
			if (Build.Model.Contains("Glass"))
			{
				resolution = new CameraResolution
				{
					Width = 640,
					Height = 360
				};
				// Glass requires 30fps
				parameters.SetPreviewFpsRange(30000, 30000);
			}

			// Hopefully a resolution was selected at some point
			if (resolution != null)
			{
				Android.Util.Log.Debug(MobileBarcodeScanner.TAG,
					"Selected Resolution: " + resolution.Width + "x" + resolution.Height);

				CameraResolution = resolution;
				parameters.SetPreviewSize(resolution.Width, resolution.Height);
			}

			Camera.SetParameters(parameters);

			SetCameraDisplayOrientation();
		}

		void AutoFocus(int x, int y, bool useCoordinates)
		{
			if (Camera == null) return;

			if (scannerHost.ScanningOptions.DisableAutofocus)
			{
				Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Disabled");
				return;
			}

			var cameraParams = Camera.GetParameters();

			Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Requested");

			// Cancel any previous requests
			Camera.CancelAutoFocus();

			try
			{
				// If we want to use coordinates
				// Also only if our camera supports Auto focus mode
				// Since FocusAreas only really work with FocusModeAuto set
				if (useCoordinates
					&& cameraParams.SupportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
				{
					// Let's give the touched area a 20 x 20 minimum size rect to focus on
					// So we'll offset -10 from the center of the touch and then 
					// make a rect of 20 to give an area to focus on based on the center of the touch
					x = x - 10;
					y = y - 10;

					// Ensure we don't go over the -1000 to 1000 limit of focus area
					if (x >= 1000)
						x = 980;
					if (x < -1000)
						x = -1000;
					if (y >= 1000)
						y = 980;
					if (y < -1000)
						y = -1000;

					// Explicitly set FocusModeAuto since Focus areas only work with this setting
					cameraParams.FocusMode = Camera.Parameters.FocusModeAuto;
					// Add our focus area
					cameraParams.FocusAreas = new List<Camera.Area>
					{
						new Camera.Area(new Rect(x, y, x + 20, y + 20), 1000)
					};
					Camera.SetParameters(cameraParams);
				}

				// Finally autofocus (weather we used focus areas or not)
				Camera.AutoFocus(cameraEventListener);
			}
			catch (Exception ex)
			{
				Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Failed: {0}", ex);
			}
		}

		void SetCameraDisplayOrientation()
		{
			var degrees = GetCameraDisplayOrientation();
			LastCameraDisplayOrientationDegree = degrees;

			Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Changing Camera Orientation to: " + degrees);

			try
			{
				Camera.SetDisplayOrientation(degrees);
			}
			catch (Exception ex)
			{
				Android.Util.Log.Error(MobileBarcodeScanner.TAG, ex.ToString());
			}
		}

		int GetCameraDisplayOrientation()
		{
			int degrees;
			var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			var display = windowManager.DefaultDisplay;
			var rotation = display.Rotation;

			switch (rotation)
			{
				case SurfaceOrientation.Rotation0:
					degrees = 0;
					break;
				case SurfaceOrientation.Rotation90:
					degrees = 90;
					break;
				case SurfaceOrientation.Rotation180:
					degrees = 180;
					break;
				case SurfaceOrientation.Rotation270:
					degrees = 270;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var info = new Camera.CameraInfo();
			Camera.GetCameraInfo(cameraId, info);

			int correctedDegrees;
			if (info.Facing == CameraFacing.Front)
			{
				correctedDegrees = (info.Orientation + degrees) % 360;
				correctedDegrees = (360 - correctedDegrees) % 360; // compensate the mirror
			}
			else
			{
				// back-facing
				correctedDegrees = (info.Orientation - degrees + 360) % 360;
			}

			return correctedDegrees;
		}
	}
}