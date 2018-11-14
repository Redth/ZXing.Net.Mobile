/*
* Copyright 2018 ZXing/Redth - https://github.com/Redth/ZXing.Net.Mobile
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
* 
* Edited by VK, Apacheta Corp 11/14/2018.
* http://www.apacheta.com/
* 
*/

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
using Android.Util;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraController
    {
        private const float MAX_EXPOSURE_COMPENSATION = 1.5f;
        private const float MIN_EXPOSURE_COMPENSATION = 0.0f;
        private const int AREA_PER_1000 = 300;
        private readonly Context _context;
        private readonly ISurfaceHolder _holder;
        private readonly SurfaceView _surfaceView;
        private readonly CameraEventsListener _cameraEventListener;
        private int _cameraId;
		IScannerSessionHost _scannerHost;

        public CameraController(SurfaceView surfaceView, CameraEventsListener cameraEventListener, IScannerSessionHost scannerHost)
        {
            _context = surfaceView.Context;
            _holder = surfaceView.Holder;
            _surfaceView = surfaceView;
            _cameraEventListener = cameraEventListener;
			_scannerHost = scannerHost;
        }

        public Camera Camera { get; private set; }

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

               Log.Debug(MobileBarcodeScanner.TAG, $"bitsPerPixed={bitsPerPixel}; bufferSize={bufferSize}");
                const int NUM_PREVIEW_BUFFERS = 5;
				for (uint i = 0; i < NUM_PREVIEW_BUFFERS; ++i)
				{
					using (var buffer = new FastJavaByteArray(bufferSize))
						Camera.AddCallbackBuffer(buffer);
				}

                

				Camera.StartPreview();

                Camera.SetNonMarshalingPreviewCallback(_cameraEventListener);
            }
            catch (Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, ex.ToString());
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


        /// <summary>
        ///Scanning Improvement, VK 10/2018
        /// </summary>
        public void LowLightMode(bool on)
        {
            var parameters = Camera?.GetParameters();
            if (parameters != null)
            {
                SetBestExposure(parameters, on);
                Camera.SetParameters(parameters);
            }
        }

        public void AutoFocus(int x, int y)
        {
            // The bounds for focus areas are actually -1000 to 1000
            // So we need to translate the touch coordinates to this scale
            var focusX = x / _surfaceView.Width * 2000 - 1000;
            var focusY = y / _surfaceView.Height * 2000 - 1000;

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
                Log.Error(MobileBarcodeScanner.TAG, e.ToString());
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

					if (_scannerHost.ScanningOptions.UseFrontCameraIfAvailable.HasValue &&
                        _scannerHost.ScanningOptions.UseFrontCameraIfAvailable.Value)
                        whichCamera = CameraFacing.Front;

                    for (var i = 0; i < numCameras; i++)
                    {
                        Camera.GetCameraInfo(i, camInfo);
                        if (camInfo.Facing == whichCamera)
                        {
                            Log.Debug(MobileBarcodeScanner.TAG,
                                "Found " + whichCamera + " Camera, opening...");
                            Camera = Camera.Open(i);
                            _cameraId = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Log.Debug(MobileBarcodeScanner.TAG,
                            "Finding " + whichCamera + " camera failed, opening camera 0...");
                        Camera = Camera.Open(0);
                        _cameraId = 0;
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

        private void ApplyCameraSettings()
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
            if (_scannerHost.ScanningOptions.DisableAutofocus)
                parameters.FocusMode = Camera.Parameters.FocusModeFixed;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
                parameters.FocusMode = Camera.Parameters.FocusModeAuto;
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich &&
                supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture))
                parameters.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousVideo))
                parameters.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeFixed))
                parameters.FocusMode = Camera.Parameters.FocusModeFixed;


            Log.Debug(MobileBarcodeScanner.TAG,
                              $"FocusMode ={parameters.FocusMode}");
            var selectedFps = parameters.SupportedPreviewFpsRange.FirstOrDefault();
            if (selectedFps != null)
            {
                Log.Debug(MobileBarcodeScanner.TAG,
                              $"Old Selected fps Min:{selectedFps[0]}, Max {selectedFps[1]}");
                // This will make sure we select a range with the lowest minimum FPS
                // and maximum FPS which still has the lowest minimum
                // This should help maximize performance / support for hardware
                //foreach (var fpsRange in parameters.SupportedPreviewFpsRange)
                //{
                //    if (fpsRange[0] < selectedFps[0] && fpsRange[1] >= selectedFps[1])
                //        selectedFps = fpsRange;
                //}

                /// <summary>
                ///Scanning Improvement, VK 10/2018
                /// </summary>
                foreach (var fpsRange in parameters.SupportedPreviewFpsRange)
                {
                    if (fpsRange[1] > selectedFps[1] || fpsRange[1] == selectedFps[1] && fpsRange[0] < selectedFps[0])
                        selectedFps = fpsRange;
                }

               Log.Debug(MobileBarcodeScanner.TAG,
                            $" Setting Selected fps to Min:{selectedFps[0]}, Max {selectedFps[1]}");

                /// <summary>
                ///Scanning Improvement, Apacheta corporation 11/14/2018
                ///Changed the fps to use low and high. instead of low value and low value ie., selectedFps[0].
                ///Old code ::  parameters.SetPreviewFpsRange(selectedFps[0], selectedFps[0]);
                /// </summary>
                parameters.SetPreviewFpsRange(selectedFps[0], selectedFps[1]);
            }
            
            if (_scannerHost.ScanningOptions.LowLightMode == true)
                SetBestExposure(parameters, parameters.FlashMode != Camera.Parameters.FlashModeOn);

            /*
             * Edited by VK - Apacheta corporation 11/14/2018
             * Improvements based on zxing android library
             * - Setting default auto focus areas instead of single focus point
             * - Setting Barcode scene mode if available for the device
             * - Set metering to improve lighting/ exposure in the focused area (i.e., rectangular focus area in the center)
             * - **** Imp ==> In UI project a layout should be created to mask other areas except the center rectangular area. 
             *                  To inform the user that app/ camera only scans the center rectangular area of the device.
             */
            SetDefaultFocusArea(parameters);
            SetBarcodeSceneMode(parameters);
            SetMetering(parameters);

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
                resolution = _scannerHost.ScanningOptions.GetResolution(availableResolutions.ToList());

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
                Log.Debug(MobileBarcodeScanner.TAG,
                    "Selected Resolution: " + resolution.Width + "x" + resolution.Height);
                parameters.SetPreviewSize(resolution.Width, resolution.Height);
            }

            Camera.SetParameters(parameters);

            SetCameraDisplayOrientation();
        }

        /// <summary>
        ///Scanning Improvement, VK, Apacheta Corp 11/14/2018.
        ///This method sets the best expsure setting for the device.
        /// </summary>
        private void SetBestExposure(Camera.Parameters parameters, bool lowLight)
        {
            int minExposure = parameters.MinExposureCompensation;
            int maxExposure = parameters.MaxExposureCompensation;
            float step = parameters.ExposureCompensationStep;
            if ((minExposure != 0 || maxExposure != 0) && step > 0.0f)
            {
                // Set low when light is on
                float targetCompensation = MAX_EXPOSURE_COMPENSATION;
                int compensationSteps = (int)(targetCompensation / step);
                float actualCompensation = step * compensationSteps;
                // Clamp value:
                compensationSteps = lowLight ? Math.Max(Math.Min(compensationSteps, maxExposure), minExposure) : (int)MIN_EXPOSURE_COMPENSATION;
                if (parameters.ExposureCompensation == compensationSteps)
                {
                    Log.Debug(MobileBarcodeScanner.TAG, "Exposure compensation already set to " + compensationSteps + " / " + actualCompensation);
                }
                else
                {
                    Log.Debug(MobileBarcodeScanner.TAG, "Setting exposure compensation to " + compensationSteps + " / " + actualCompensation);
                    parameters.ExposureCompensation = compensationSteps;
                }
            }
            else
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Camera does not support exposure compensation");
            }
        }

        /// <summary>
        ///Scanning Improvement, VK Apacheta Corp 11/14/2018.
        ///This method sets the focus area setting for the device. center rectangle
        /// </summary>
        private void SetDefaultFocusArea(Camera.Parameters parameters)
        {
            if (parameters?.MaxNumFocusAreas > 0)
            {
                List<Camera.Area> middleArea = BuildMiddleArea(AREA_PER_1000);
                Log.Debug(MobileBarcodeScanner.TAG, "Setting focus area to : " + middleArea.Select(f => f.Rect.FlattenToString()).Aggregate((first, next) => first + "; " + next));
                parameters.FocusAreas = middleArea;
            }
            else
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Device does not support focus areas");
            }
        }


        /// <summary>
        ///Scanning Improvement, VK Apacheta Corp 11/14/2018.
        ///This method sets the meter setting for the device. center rectangle
        /// </summary>
        private void SetMetering(Camera.Parameters parameters)
        {
            if (parameters?.MaxNumMeteringAreas > 0)
            {
                List<Camera.Area> middleArea = BuildMiddleArea(AREA_PER_1000);
                Log.Debug(MobileBarcodeScanner.TAG, "Setting metering areas: " + middleArea.Select(f => f.Rect.FlattenToString()).Aggregate((first, next) => first + "; " + next));
                parameters.MeteringAreas = middleArea;
            }
            else
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Device does not support metering areas");
            }
        }

        /// <summary>
        ///Scanning Improvement, VK Apacheta Corp 11/14/2018.
        ///This method builds the middle are i.e., center rectangle for the device
        /// </summary>
        private List<Camera.Area> BuildMiddleArea(int areaPer1000)
        {
            return new List<Camera.Area>()
                {
                    new Camera.Area(new Rect(-areaPer1000, -areaPer1000, areaPer1000, areaPer1000), 1)
                };
        }


        /// <summary>
        ///Scanning Improvement, VK Apacheta Corp 11/14/2018.
        ///This method sets the Video stabilization setting for the device. 
        ///This method is not used in the code for now. 
        /// </summary>
        private void SetVideoStabilization(Camera.Parameters parameters)
        {
            if (parameters.IsVideoStabilizationSupported)
            {
                if (parameters.VideoStabilization)
                {
                    Log.Debug(MobileBarcodeScanner.TAG, "Video stabilization already enabled");
                }
                else
                {
                    Log.Debug(MobileBarcodeScanner.TAG, "Enabling video stabilization...");
                    parameters.VideoStabilization = true;
                }
            }
            else
            {
                Log.Debug(MobileBarcodeScanner.TAG, "This device does not support video stabilization");
            }
        }

        /// <summary>
        ///Scanning Improvement, VK Apacheta Corp 11/14/2018.
        ///This method sets the scene to barcode for the device. If the device supports scenes.
        /// </summary>
        private void SetBarcodeSceneMode(Camera.Parameters parameters)
        {
            if (parameters.SceneMode ==  Camera.Parameters.SceneModeBarcode)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Barcode scene mode already set");
                return;
            }
            var supportedSceneModes = parameters.SupportedSceneModes;
            if (supportedSceneModes?.Contains(Camera.Parameters.SceneModeBarcode) == true)
            {
                Log.Debug(MobileBarcodeScanner.TAG, $"Previous SceneMode={parameters.SceneMode}");
                parameters.SceneMode = Camera.Parameters.SceneModeBarcode;
                Log.Debug(MobileBarcodeScanner.TAG, "Barcode scene mode is set");
            }
           
        }

       private void SetZoom(Camera.Parameters parameters, double targetZoomRatio)
        {
            if (parameters.IsZoomSupported)
            {
                var zoom = IndexOfClosestZoom(parameters, targetZoomRatio);
                if (zoom == null)
                {
                    return;
                }
                if (parameters.Zoom == zoom)
                {
                    Log.Debug(MobileBarcodeScanner.TAG, "Zoom is already set to " + zoom);
                }
                else
                {
                    Log.Debug(MobileBarcodeScanner.TAG, "Setting zoom to " + zoom);
                    parameters.Zoom = (int)zoom;
                }
            }
            else
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Zoom is not supported");
            }
        }

        private int? IndexOfClosestZoom(Camera.Parameters parameters, double targetZoomRatio)
        {
            var ratios = parameters.ZoomRatios.ToList();
            Log.Debug(MobileBarcodeScanner.TAG, "Zoom ratios: " + ratios);
            int maxZoom = parameters.MaxZoom;
            if (ratios == null || ratios.Count == 0 || ratios.Count != maxZoom + 1)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Invalid zoom ratios!");
                return null;
            }
            double target100 = 100.0 * targetZoomRatio;
            double smallestDiff = Double.PositiveInfinity;
            int closestIndex = 0;
            for (int i = 0; i < ratios.Count; i++)
            {
                double diff = Math.Abs(ratios[i]?.LongValue() ?? 0 - target100);
                if (diff < smallestDiff)
                {
                    smallestDiff = diff;
                    closestIndex = i;
                }
            }
            Log.Debug(MobileBarcodeScanner.TAG, "Chose zoom ratio of " + ((ratios[closestIndex]?.LongValue() ?? 0) / 100.0));
            return closestIndex;
        }

        private void AutoFocus(int x, int y, bool useCoordinates)
        {
            if (Camera == null) return;

			if (_scannerHost.ScanningOptions.DisableAutofocus)
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
                    Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"AutoFocus Area =(x={x}, y={y}, right = {x + 20}, bottom ={y + 20})");
                    Camera.SetParameters(cameraParams);
                }

                // Finally autofocus (weather we used focus areas or not)
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
