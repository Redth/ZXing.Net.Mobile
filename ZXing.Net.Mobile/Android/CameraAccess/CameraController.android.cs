using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
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
            // The bounds for focus areas are actually -1000 to 1000
            // So we need to translate the touch coordinates to this scale
            var focusX = x / surfaceView.Width * 2000 - 1000;
            var focusY = y / surfaceView.Height * 2000 - 1000;

            // Call the autofocus with our coords
            AutoFocus(focusX, focusY, true);
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

                // 1050 and 1400 are a random guess which work pretty good
                var idealSize = GetOptimalSize(supportedSizes, 1050, 1400);
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

        public void StartPreview()
        {
            if (Camera is null || holder is null || imageReader is null || backgroundHandler is null) return;

            try
            {
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

            var aspectTolerance = 0.1;
            var targetRatio = (double)width / height;

            Size optimalSize = null;
            var minDiff = double.MaxValue;
            var targetHeight = height;

            while (optimalSize is null)
            {
                foreach (var size in sizes)
                {
                    var ratio = (double)size.Width / size.Height;

                    if (System.Math.Abs(ratio - targetRatio) > aspectTolerance)
                        continue;

                    if (System.Math.Abs(size.Height - targetHeight) < minDiff)
                    {
                        optimalSize = size;
                        minDiff = System.Math.Abs(size.Height - targetHeight);
                    }
                }

                if (optimalSize == null)
                    aspectTolerance += 0.1f;
            }

            return optimalSize;
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
