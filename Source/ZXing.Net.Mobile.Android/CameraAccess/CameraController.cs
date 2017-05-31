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
        private readonly Context _context;
        private readonly MobileBarcodeScanningOptions _scanningOptions;
        private readonly ISurfaceHolder _holder;
        private readonly CameraEventsListener _cameraEventListener;
        private int _cameraId;
        private bool _autoFocusCycleDone = true;
        private bool _useContinousFocus;

        public CameraController(SurfaceView surfaceView, CameraEventsListener cameraEventListener,
            MobileBarcodeScanningOptions scanningOptions)
        {
            SurfaceView = surfaceView;

            _context = surfaceView.Context;
            _scanningOptions = scanningOptions;
            _holder = surfaceView.Holder;

            _cameraEventListener = cameraEventListener;
            _cameraEventListener.AutoFocus += (s, e) => 
                _autoFocusCycleDone = true;
        }

        public SurfaceView SurfaceView { get; }

        public Camera Camera { get; private set; }

        public event EventHandler<FastJavaByteArray> OnPreviewFrameReady
        {
            add { _cameraEventListener.OnPreviewFrameReady += value; }
            remove { _cameraEventListener.OnPreviewFrameReady -= value; }
        }

        public int LastCameraDisplayOrientationDegree { get; private set; }

        public void RefreshCamera()
        {
            if (_holder == null) return;

            ApplyCameraSettings();

            try
            {
                Camera.SetPreviewDisplay(_holder);
                Camera.StartPreview();
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, ex.ToString());
            }
        }

        public void SetupCamera()
        {
            if (Camera != null) return;

            ZXing.Net.Mobile.Android.PermissionsHandler.CheckCameraPermissions(_context);

            var perf = PerformanceCounter.Start();
            OpenCamera();
            PerformanceCounter.Stop(perf, "Setup Camera took {0}ms");

            if (Camera == null) return;

            perf = PerformanceCounter.Start();
            ApplyCameraSettings();

            try
            {
                Camera.SetPreviewDisplay(_holder);
                

                var previewParameters = Camera.GetParameters();
                var previewSize = previewParameters.PreviewSize;
                var bitsPerPixel = ImageFormat.GetBitsPerPixel(previewParameters.PreviewFormat);


                int bufferSize = (previewSize.Width * previewSize.Height * bitsPerPixel) / 8;
				using (var buffer = new FastJavaByteArray(bufferSize))
					Camera.AddCallbackBuffer(buffer);
                

				Camera.StartPreview();

                Camera.SetNonMarshalingPreviewCallback(_cameraEventListener);
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
            var focusX = x / SurfaceView.Width * 2000 - 1000;
            var focusY = y / SurfaceView.Height * 2000 - 1000;

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
                    Camera.SetPreviewDisplay(null);
                    Camera.StopPreview();
                    Camera.SetNonMarshalingPreviewCallback(null); // replaces Camera.SetPreviewCallback(null);
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

        private void OpenCamera()
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

                    if (_scanningOptions.UseFrontCameraIfAvailable.HasValue &&
                        _scanningOptions.UseFrontCameraIfAvailable.Value)
                        whichCamera = CameraFacing.Front;

                    for (var i = 0; i < numCameras; i++)
                    {
                        Camera.GetCameraInfo(i, camInfo);
                        if (camInfo.Facing == whichCamera)
                        {
                            Android.Util.Log.Debug(MobileBarcodeScanner.TAG,
                                "Found " + whichCamera + " Camera, opening...");
                            Camera = Camera.Open(i);
                            _cameraId = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Android.Util.Log.Debug(MobileBarcodeScanner.TAG,
                            "Finding " + whichCamera + " camera failed, opening camera 0...");
                        Camera = Camera.Open(0);
                        _cameraId = 0;
                    }
                }
                else
                {
                    Camera = Camera.Open();
                }
            }
            catch (Exception ex)
            {
                ShutdownCamera();
                MobileBarcodeScanner.LogError("Setup Error: {0}", ex);
            }
        }

        private void ApplyCameraSettings()
        {
            var parameters = Camera.GetParameters();
            parameters.PreviewFormat = ImageFormatType.Nv21; // YCrCb format (all Android devices must support this)

            // Android actually defines a barcode scene mode ..
            if (parameters.SupportedSceneModes.Contains(Camera.Parameters.SceneModeBarcode)) // .. we might be lucky :-)
                parameters.SceneMode = Camera.Parameters.SceneModeBarcode;

            // First try continuous video, then auto focus, then fixed
            var supportedFocusModes = parameters.SupportedFocusModes;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich &&
                supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture))
                parameters.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousVideo))
                parameters.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
                parameters.FocusMode = Camera.Parameters.FocusModeAuto;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeFixed))
                parameters.FocusMode = Camera.Parameters.FocusModeFixed;

            var availableResolutions = parameters.SupportedPreviewSizes.Select(sps => new CameraResolution
            {
                Width = sps.Width,
                Height = sps.Height
            });

            // Try and get a desired resolution from the options selector
            var resolution = _scanningOptions.GetResolution(availableResolutions.ToList());

            // If the user did not specify a resolution, let's try and find a suitable one
            if (resolution == null)
            {
                foreach (var sps in parameters.SupportedPreviewSizes)
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
                parameters.SetPreviewSize(resolution.Width, resolution.Height);
            }

            Camera.SetParameters(parameters);

            parameters = Camera.GetParameters(); // refresh to see what is actually set!

            _useContinousFocus = parameters.FocusMode == Camera.Parameters.FocusModeContinuousPicture || parameters.FocusMode == Camera.Parameters.FocusModeContinuousVideo;

            SetCameraDisplayOrientation();
        }

        private void AutoFocus(int x, int y, bool useCoordinates)
        {
            if (_useContinousFocus || !_autoFocusCycleDone || Camera == null) 
                return;

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
                    y = y - 10; // todo: ensure positive!

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
                _autoFocusCycleDone = false;
                Camera.AutoFocus(_cameraEventListener);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Failed: {0}", ex);
            }
        }

        private void SetCameraDisplayOrientation()
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

        private int GetCameraDisplayOrientation()
        {
            int degrees;
            var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
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
            Camera.GetCameraInfo(_cameraId, info);

            int correctedDegrees;
            if (info.Facing == CameraFacing.Front)
            {
                correctedDegrees = (info.Orientation + degrees)%360;
                correctedDegrees = (360 - correctedDegrees)%360; // compensate the mirror
            }
            else
            {
                // back-facing
                correctedDegrees = (info.Orientation - degrees + 360)%360;
            }

            return correctedDegrees;
        }
    }
}