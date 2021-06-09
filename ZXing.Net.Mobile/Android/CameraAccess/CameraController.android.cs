﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraController
    {
        readonly Context context;
        readonly ISurfaceHolder holder;
        readonly SurfaceView surfaceView;
        readonly CameraEventsListener cameraEventListener;
        readonly IScannerSessionHost scannerHost;
        readonly CameraStateCallback cameraStateCallback;

        CameraManager cameraManager;
        ImageReader imageReader;
        bool flashSupported;
        Handler backgroundHandler;
        CaptureRequest.Builder previewBuilder;
        CameraCaptureSession previewSession;
        CaptureRequest previewRequest;
        HandlerThread backgroundThread;

        public string CameraId { get; private set; }

        public bool OpeningCamera { get; private set; }

        public CameraDevice Camera { get; private set; }

        public Size IdealPhotoSize { get; private set; }

        public CameraController(SurfaceView surfaceView, CameraEventsListener cameraEventListener, IScannerSessionHost scannerHost)
        {
            context = surfaceView.Context;
            holder = surfaceView.Holder;
            this.surfaceView = surfaceView;
            this.cameraEventListener = cameraEventListener;
            this.scannerHost = scannerHost;
            cameraStateCallback = new CameraStateCallback()
            {
                OnErrorAction = (camera, error) =>
                {
                    camera.Close();

                    Camera = null;
                    OpeningCamera = false;

                    Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Error on opening camera: " + error);
                },
                OnOpenedAction = camera =>
                {
                    Camera = camera;
                    StartPreview();
                    OpeningCamera = false;
                },
                OnDisconnectedAction = camera =>
                {
                    camera.Close();
                    Camera = null;
                    OpeningCamera = false;
                }
            };
        }

        public void RefreshCamera()
        {
            if (Camera is null || previewRequest is null || previewSession is null || previewBuilder is null) return;

            SetUpCameraOutputs();
            previewRequest.Dispose();
            previewSession.Dispose();
            previewBuilder.Dispose();
            StartPreview();
        }

        public void SetupCamera()
        {
            StartBackgroundThread();

            OpenCamera();
        }

        public void ShutdownCamera()
        {
            if (Camera != null)
                Camera.Close();

            StopBackgroundThread();
        }

        public void AutoFocus()
        {
            AutoFocus(0, 0, false);
        }

        public void AutoFocus(int x, int y)
        {
            // Call the autofocus with our coords
            AutoFocus(x, y, true);
        }

        void AutoFocus(int x, int y, bool useCoordinates)
        {
            if (Camera == null) return;

            try
            {
                if (scannerHost.ScanningOptions.DisableAutofocus)
                {
                    Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Disabled");
                    return;
                }

                var characteristics = cameraManager.GetCameraCharacteristics(CameraId.ToString());
                var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                var supportedFocusModes = ((int[])characteristics
                    .Get(CameraCharacteristics.ControlAfAvailableModes))
                    .Select(x => (ControlAFMode)x);

                Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Requested");

                // If we want to use coordinates
                // Also only if our camera supports Auto focus mode
                // Since FocusAreas only really work with FocusModeAuto set
                if (useCoordinates && supportedFocusModes.Contains(ControlAFMode.Auto))
                {
                    // Let's give the touched area a 20 x 20 minimum size rect to focus on
                    // So we'll offset -10 from the center of the touch and then
                    // make a rect of 20 to give an area to focus on based on the center of the touch
                    x = x - 10;
                    y = y - 10;

                    // Explicitly set FocusModeAuto since Focus areas only work with this setting
                    previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Auto);
                    // Add our focus area
                    previewBuilder.Set(CaptureRequest.ControlAfRegions, new MeteringRectangle[]
                    {
                        new MeteringRectangle(x, y, x + 20, y + 20, 1000)
                    });

                    previewBuilder.Set(CaptureRequest.ControlAeRegions, new MeteringRectangle[]
                    {
                        new MeteringRectangle(x, y, x + 20, y + 20, 1000)
                    });
                }

                // Finally autofocus (weather we used focus areas or not)
                previewBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Start);

                UpdatePreview();
            }
            catch (System.Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Failed: {0}", ex);
            }
        }

        void SetUpCameraOutputs()
        {
            try
            {
                cameraManager = (CameraManager)context.GetSystemService(Context.CameraService);

                var cameraIds = cameraManager.GetCameraIdList();

                CameraId = cameraIds[0];

                var whichCamera = LensFacing.Back;

                if (scannerHost.ScanningOptions.UseFrontCameraIfAvailable.HasValue &&
                    scannerHost.ScanningOptions.UseFrontCameraIfAvailable.Value)
                    whichCamera = LensFacing.Front;

                for (var i = 0; i < cameraIds.Length; i++)
                {
                    var cameraCharacteristics = cameraManager.GetCameraCharacteristics(cameraIds[i]);

                    var facing = (Integer)cameraCharacteristics.Get(CameraCharacteristics.LensFacing);
                    if (facing != null && facing.IntValue() == (int)whichCamera)
                    {
                        CameraId = cameraIds[i];
                        break;
                    }
                }

                var characteristics = cameraManager.GetCameraCharacteristics(CameraId);
                var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                Size[] supportedSizes = null;

                if (characteristics != null)
                    supportedSizes = ((StreamConfigurationMap)characteristics
                        .Get(CameraCharacteristics.ScalerStreamConfigurationMap))
                        .GetOutputSizes((int)ImageFormatType.Yuv420888);

                if (supportedSizes is null || supportedSizes.Length == 0)
                {
                    Log.Debug(MobileBarcodeScanner.TAG, "Failed to get supported output sizes");
                    return;
                }

                var wm = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
                var display = wm.DefaultDisplay;
                var point = new Point();
                display.GetSize(point);
                var idealSize = point.X > point.Y ? GetOptimalSize(supportedSizes, point.X, point.Y) : GetOptimalSize(supportedSizes, point.Y, point.X);
                imageReader = ImageReader.NewInstance(idealSize.Width, idealSize.Height, ImageFormatType.Yuv420888, 5);

                flashSupported = HasFLash(characteristics);

                imageReader.SetOnImageAvailableListener(cameraEventListener, backgroundHandler);

                IdealPhotoSize = idealSize;
            }
            catch (System.Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Could not setup camera outputs" + ex);
            }
        }

        bool HasFLash(CameraCharacteristics characteristics)
        {
            var available = (Java.Lang.Boolean)characteristics.Get(CameraCharacteristics.FlashInfoAvailable);
            if (available == null)
            {
                return false;
            }
            else
            {
                return (bool)available;
            }
        }

        public void OpenCamera()
        {
            if (context == null || OpeningCamera)
            {
                return;
            }

            try
            {
                OpeningCamera = true;

                SetUpCameraOutputs();

                cameraManager.OpenCamera(CameraId, cameraStateCallback, backgroundHandler);
            }
            catch (System.Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Error on opening camera" + ex);
            }
        }

        Size GetOptimalPreviewSize(SurfaceView surface)
        {
            var width = surface.Width > surface.Height ? surface.Width : surface.Height;
            var height = surface.Width > surface.Height ? surface.Height : surface.Width;
            var characteristics = cameraManager.GetCameraCharacteristics(CameraId);
            var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            var availableSizes = ((StreamConfigurationMap)characteristics
                        .Get(CameraCharacteristics.ScalerStreamConfigurationMap))
                        .GetOutputSizes(Class.FromType(typeof(ISurfaceHolder)));

            var aspectRatio = (double)width / (double)height;
            var availableAspectRatios = availableSizes.Select(x => (x, (double)x.Width / (double)x.Height));

            var differences = availableAspectRatios.Select(x => (x.x, System.Math.Abs(x.Item2 - aspectRatio)));
            var bestMatches = differences.OrderBy(x => x.Item2).ThenBy(x => System.Math.Abs(x.x.Width - width)).ThenBy(x => System.Math.Abs(x.x.Height - height)).Take(5);
            return bestMatches.OrderByDescending(x => x.x.Width).ThenByDescending(x => x.x.Height).First().x;
        }

        public void StartPreview()
        {
            if (Camera is null || holder is null || imageReader is null || backgroundHandler is null) return;

            try
            {
                var optimalPreviewSize = GetOptimalPreviewSize(surfaceView);

                if (Looper.MyLooper() == Looper.MainLooper)
                {
                    holder.SetFixedSize(optimalPreviewSize.Width, optimalPreviewSize.Height);
                }
                else
                {
                    var sizeSetResetEvent = new ManualResetEventSlim(false);
                    using (var handler = new Handler(Looper.MainLooper))
                    {
                        handler.Post(() =>
                        {
                            holder.SetFixedSize(optimalPreviewSize.Width, optimalPreviewSize.Height);
                            sizeSetResetEvent.Set();
                        });
                    }

                    sizeSetResetEvent.Wait();
                    sizeSetResetEvent.Reset();
                }

                // This is needed bc otherwise the preview is sometimes distorted
                System.Threading.Thread.Sleep(30);

                previewBuilder = Camera.CreateCaptureRequest(CameraTemplate.Preview);
                previewBuilder.AddTarget(holder.Surface);
                previewBuilder.AddTarget(imageReader.Surface);

                var surfaces = new List<Surface>();
                surfaces.Add(holder.Surface);
                surfaces.Add(imageReader.Surface);

                Camera.CreateCaptureSession(surfaces,
                    new CameraCaptureStateListener
                    {
                        OnConfigureFailedAction = session =>
                        {
                        },
                        OnConfiguredAction = session =>
                        {
                            previewSession = session;
                            UpdatePreview();
                        }
                    },
                    backgroundHandler);
            }
            catch (System.Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Error on starting preview" + ex);
            }
        }

        void UpdatePreview()
        {
            if (Camera is null || previewSession is null) return;

            try
            {
                previewRequest = previewBuilder.Build();
                previewSession.SetRepeatingRequest(previewRequest, null, backgroundHandler);
            }
            catch (System.Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Error on updating preview" + ex);
            }
        }

        Size GetOptimalSize(IList<Size> sizes, int width, int height)
        {
            if (sizes is null) return null;

            var aspectRatio = (double)width / (double)height;
            var availableAspectRatios = sizes.Select(x => (x, (double)x.Width / (double)x.Height));

            var differences = availableAspectRatios.Select(x => (x.x, System.Math.Abs(x.Item2 - aspectRatio)));
            var bestMatches = differences.OrderBy(x => x.Item2).ThenBy(x => System.Math.Abs(x.x.Width - width)).ThenBy(x => System.Math.Abs(x.x.Height - height)).Take(5);
            var orderedMatches = bestMatches.OrderBy(x => x.x.Width).ThenBy(x => x.x.Height);
            return orderedMatches.First().x;
        }

        void StartBackgroundThread()
        {
            backgroundThread = new HandlerThread("CameraBackgroundThread");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
        }

        public void EnableTorch(bool state)
        {
            try
            {
                if (!flashSupported || previewBuilder is null) return;

                if (state)
                {
                    previewBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.On);
                    previewBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Torch);
                }
                else
                    previewBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Off);

                UpdatePreview();
            }
            catch (System.Exception ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Error on enabling torch" + ex);
            }
        }

        void StopBackgroundThread()
        {
            try
            {
                backgroundThread?.QuitSafely();
                backgroundThread?.Join();
                backgroundThread = null;
                backgroundHandler = null;
            }
            catch (InterruptedException ex)
            {
                Log.Debug(MobileBarcodeScanner.TAG, "Error stopping background threads: " + ex);
            }
        }
    }
}
