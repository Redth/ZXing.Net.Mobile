using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using Java.Lang;
using Java.Util.Concurrent;

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
        private Size[] supportedJpegSizes;
        private Size idealPhotoSize;
        private ImageReader imageReader;
        private bool flashSupported;
        private Handler backgroundHandler;
        private CaptureRequest.Builder previewBuilder;
        private CameraCaptureSession previewSession;
        private CaptureRequest previewRequest;
        private HandlerThread backgroundThread;
        private int? lastCameraDisplayOrientationDegree;

        public string CameraId { get; private set; }

        public bool OpeningCamera { get; private set; }

        public CameraDevice Camera { get; private set; }

        public Size PreviewSize { get; private set; }

        public Size IdealPhotoSize { get; private set; }

        public int LastCameraDisplayOrientationDegree
        {
            get
            {
                if (lastCameraDisplayOrientationDegree is null)
                {
                    lastCameraDisplayOrientationDegree = GetCameraDisplayOrientation();
                }

                return lastCameraDisplayOrientationDegree.Value;
            }
        }

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

        public void RefreshCamera(int width, int height)
        {
            if (Camera is null || previewRequest is null || previewSession is null || previewBuilder is null) return;
            SetUpCameraOutputs(width, height);
            previewRequest.Dispose();
            previewSession.Dispose();
            previewBuilder.Dispose();
            StartPreview();
        }

        public void SetupCamera(int width, int height)
        {
            StartBackgroundThread();

            OpenCamera(width, height);
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

            var characteristics = cameraManager.GetCameraCharacteristics(CameraId);
            var facing = (int)characteristics.Get(CameraCharacteristics.LensFacing);
            var orientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);
            int correctedDegrees;
            if (facing == (int)CameraFacing.Front)
            {
                correctedDegrees = (orientation + degrees) % 360;
                correctedDegrees = (360 - correctedDegrees) % 360; // compensate the mirror
            }
            else
            {
                // back-facing
                correctedDegrees = (orientation - degrees + 360) % 360;
            }

            return correctedDegrees;
        }

        void AutoFocus(int x, int y, bool useCoordinates)
        {
            if (Camera == null) return;

            if (scannerHost.ScanningOptions.DisableAutofocus)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Disabled");
                return;
            }

            var characteristics = cameraManager.GetCameraCharacteristics(CameraId.ToString());
            var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            var supportedFocusModes = ((int[])characteristics
                .Get(CameraCharacteristics.ControlAfAvailableModes))
                .Select(x => (ControlAFMode)x);

            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Requested");

            try
            {
                // If we want to use coordinates
                // Also only if our camera supports Auto focus mode
                // Since FocusAreas only really work with FocusModeAuto set
                if (useCoordinates
                    && supportedFocusModes.Contains(ControlAFMode.Auto))
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
            }
            catch (System.Exception ex)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Failed: {0}", ex);
            }
        }

        void SetUpCameraOutputs(int width, int height)
        {
            cameraManager = (CameraManager)context.GetSystemService(Context.CameraService);

            var cameraIds = cameraManager.GetCameraIdList();

            CameraId = cameraIds[0];

            for (var i = 0; i < cameraIds.Length; i++)
            {
                var cameraCharacteristics = cameraManager.GetCameraCharacteristics(cameraIds[i]);

                var facing = (Integer)cameraCharacteristics.Get(CameraCharacteristics.LensFacing);
                if (facing != null && facing == (Integer.ValueOf((int)LensFacing.Back)))
                {
                    CameraId = cameraIds[i];

                    //Phones like Galaxy S10 have 2 or 3 frontal cameras usually the one with flash is the one
                    //that should be chosen, if not It will select the first one and that can be the fish
                    //eye camera
                    if (HasFLash(cameraCharacteristics))
                        break;
                }
            }

            var characteristics = cameraManager.GetCameraCharacteristics(CameraId);
            var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

            if (supportedJpegSizes == null && characteristics != null)
            {
                supportedJpegSizes = ((StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap)).GetOutputSizes((int)ImageFormatType.Yuv420888);
            }

            if (supportedJpegSizes != null && supportedJpegSizes.Length > 0)
            {
                idealPhotoSize = GetOptimalSize(supportedJpegSizes, 1050, 1400); //MAGIC NUMBER WHICH HAS PROVEN TO BE THE BEST
            }

            imageReader = ImageReader.NewInstance(idealPhotoSize.Width, idealPhotoSize.Height, ImageFormatType.Yuv420888, 5);

            flashSupported = HasFLash(characteristics);

            imageReader.SetOnImageAvailableListener(cameraEventListener, backgroundHandler);

            IdealPhotoSize = idealPhotoSize;

            PreviewSize = GetOptimalSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))), width, height);

            lastCameraDisplayOrientationDegree = null;
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

        public void OpenCamera(int width, int height)
        {
            if (context == null || OpeningCamera)
            {
                return;
            }

            OpeningCamera = true;

            SetUpCameraOutputs(width, height);

            cameraManager.OpenCamera(CameraId, cameraStateCallback, backgroundHandler);
        }

        public void StartPreview()
        {
            if (Camera == null || PreviewSize == null) return;

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

        void UpdatePreview()
        {
            if (Camera == null || previewSession == null) return;

            // Reset the auto-focus trigger
            previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
            //SetAutoFlash(previewBuilder);

            previewRequest = previewBuilder.Build();
            previewSession.SetRepeatingRequest(previewRequest, null, backgroundHandler);
        }

        Size GetOptimalSize(IList<Size> sizes, int h, int w)
        {
            var AspectTolerance = 0.1;
            var targetRatio = (double)w / h;

            if (sizes == null)
            {
                return null;
            }

            Size optimalSize = null;
            var minDiff = double.MaxValue;
            var targetHeight = h;

            while (optimalSize == null)
            {
                foreach (var size in sizes)
                {
                    var ratio = (double)size.Width / size.Height;

                    if (System.Math.Abs(ratio - targetRatio) > AspectTolerance)
                        continue;
                    if (System.Math.Abs(size.Height - targetHeight) < minDiff)
                    {
                        optimalSize = size;
                        minDiff = System.Math.Abs(size.Height - targetHeight);
                    }
                }

                if (optimalSize == null)
                    AspectTolerance += 0.1f;
            }

            return optimalSize;
        }

        public void SetAutoFlash(CaptureRequest.Builder requestBuilder)
        {
            if (flashSupported)
            {
                requestBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.OnAutoFlash);
            }
        }

        void StartBackgroundThread()
        {
            backgroundThread = new HandlerThread("CameraBackground");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
        }

        public void EnableTorch(bool state)
        {
            if (state)
            {
                previewBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.On);
                previewBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Torch);
            }
            else
            {
                previewBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Off);
            }

            UpdatePreview();
        }

        void StopBackgroundThread()
        {
            if (backgroundHandler == null || backgroundThread == null) return;

            backgroundThread.QuitSafely();
            try
            {
                backgroundThread.Join();
                backgroundThread = null;
                backgroundHandler = null;
            }
            catch (InterruptedException e)
            {
                e.PrintStackTrace();
            }
        }
    }

    public class CameraCaptureStateListener : CameraCaptureSession.StateCallback
    {
        public Action<CameraCaptureSession> OnConfigureFailedAction;

        public Action<CameraCaptureSession> OnConfiguredAction;

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            OnConfigureFailedAction?.Invoke(session);
        }

        public override void OnConfigured(CameraCaptureSession session)
        {
            OnConfiguredAction?.Invoke(session);
        }
    }

    public class CameraStateCallback : CameraDevice.StateCallback
    {
        public Action<CameraDevice> OnDisconnectedAction;
        public Action<CameraDevice, Android.Hardware.Camera2.CameraError> OnErrorAction;
        public Action<CameraDevice> OnOpenedAction;

        public override void OnDisconnected(CameraDevice camera) => OnDisconnectedAction?.Invoke(camera);

        public override void OnError(CameraDevice camera, [GeneratedEnum] Android.Hardware.Camera2.CameraError error) => OnErrorAction?.Invoke(camera, error);

        public override void OnOpened(CameraDevice camera)
            => OnOpenedAction?.Invoke(camera);
    }
}
