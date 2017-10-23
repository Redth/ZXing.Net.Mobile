using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using ApxLabs.FastAndroidCamera;
using ZXing.Net.Mobile.Android;
using Camera = Android.Hardware.Camera;
using Matrix = Android.Graphics.Matrix;

namespace ZXing.Mobile
{
    public static class IntEx
    {
        public static bool Between(this int i, int lower, int upper)
        {
            return lower <= i && i <= upper;
        }
    }

    public static class HandlerEx
    {
        public static void PostSafe(this Handler self, Action action)
        {
            self.Post(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    // certain death, unless we squash
                    Log.Debug(MobileBarcodeScanner.TAG, $"Squashing: {ex} to avoid certain death! Handler is: {self.GetHashCode()}");
                }
            });
        }

        public static void PostSafe(this Handler self, Func<Task> action)
        {
            self.Post(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    // certain death, unless we squash
                    Log.Debug(MobileBarcodeScanner.TAG, $"Squashing: {ex} to avoid certain death! Handler is: {self.GetHashCode()}");
                }
            });
        }

    }

    public static class RectFEx
    {
        public static void Flip(this RectF s)
        {
            var tmp = s.Left;
            s.Left = s.Top;
            s.Top = tmp;
            tmp = s.Right;
            s.Right = s.Bottom;
            s.Bottom = tmp;
        }
    }

    class MyOrientationEventListener : OrientationEventListener
    {
        public MyOrientationEventListener(Context context, SensorDelay delay) : base(context, delay) { }

        public event Action<int> OrientationChanged;

        public override void OnOrientationChanged(int orientation)
        {
            try
            {
                OrientationChanged?.Invoke(orientation);
            }
            catch (Exception ex)
            {
                Log.Warn(MobileBarcodeScanner.TAG, $"Squashing {ex} in OnOrientationChanged!");
            }
        }
    }

    public class ZXingTextureView : TextureView, IScannerView, Camera.IAutoFocusCallback, INonMarshalingPreviewCallback
    {
        Camera.CameraInfo _cameraInfo;
        Camera _camera;

        static ZXingTextureView()
        {
        }

        public ZXingTextureView(IntPtr javaRef, JniHandleOwnership transfer) : base(javaRef, transfer)
        {
            Init();
        }

        public ZXingTextureView(Context ctx) : base(ctx)
        {
            Init();
        }

        public ZXingTextureView(Context ctx, MobileBarcodeScanningOptions options) : base(ctx)
        {
            Init();
            ScanningOptions = options;
        }

        public ZXingTextureView(Context ctx, IAttributeSet attr) : base(ctx, attr)
        {
            Init();
        }

        public ZXingTextureView(Context ctx, IAttributeSet attr, int defStyle) : base(ctx, attr, defStyle)
        {
            Init();
        }

        Toast _toast;
        Handler _handler;
        MyOrientationEventListener _orientationEventListener;
        TaskCompletionSource<SurfaceTextureAvailableEventArgs> _surfaceAvailable = new TaskCompletionSource<SurfaceTextureAvailableEventArgs>();
        void Init()
        {
            _toast = Toast.MakeText(Context, string.Empty, ToastLength.Short);

            var handlerThread = new HandlerThread("ZXingTextureView");
            handlerThread.Start();
            _handler = new Handler(handlerThread.Looper);

            // We have to handle changes to screen orientation explicitly, as we cannot rely on OnConfigurationChanges
            _orientationEventListener = new MyOrientationEventListener(Context, SensorDelay.Normal);
            _orientationEventListener.OrientationChanged += OnOrientationChanged;
            if (_orientationEventListener.CanDetectOrientation())
                _orientationEventListener.Enable();

            SurfaceTextureAvailable += (sender, e) =>
                _surfaceAvailable.SetResult(e);

            SurfaceTextureSizeChanged += (sender, e) =>
                SetSurfaceTransform(e.Surface, e.Width, e.Height);

            SurfaceTextureDestroyed += (sender, e) =>
            {
                ShutdownCamera();
                _surfaceAvailable = new TaskCompletionSource<SurfaceTextureAvailableEventArgs>();
            };
        }

        Camera.Size PreviewSize { get; set; }

        int _lastOrientation;
        SurfaceOrientation _lastSurfaceOrientation;
        void OnOrientationChanged(int orientation)
        {
            //
            // This code should only run when UI snaps into either portrait or landscape mode.
            // At first glance we could just override OnConfigurationChanged, but unfortunately 
            // a rotation from landscape directly to reverse landscape won't fire an event 
            // (which is easily done by rotating via upside-down on many devices), because Android
            // can just reuse the existing config and handle the rotation automatically ..
            // 
            // .. except of course for camera orientation, which must handled explicitly *sigh*. 
            // Hurray Google, you sure suck at API design!
            //
            // Instead we waste some CPU by tracking orientation down to the last degree, every 200ms.
            // I have yet to come up with a better way. 
            //
            if (_camera == null)
                return;

            var o = (((orientation + 45) % 360) / 90) * 90; // snap to 0, 90, 180, or 270.
            if (o == _lastOrientation)
                return; // fast path, no change ..

            // Actual snap is delayed, so check if we are actually rotated
            var rotation = WindowManager.DefaultDisplay.Rotation;
            if (rotation == _lastSurfaceOrientation)
                return;  // .. still no change

            _lastOrientation = o;
            _lastSurfaceOrientation = rotation;

            _handler.PostSafe(() =>
            {
                _camera?.SetDisplayOrientation(CameraOrientation(WindowManager.DefaultDisplay.Rotation)); // and finally, the interesting part *sigh*
            });
        }

        bool IsPortrait
        {
            get
            {
                var rotation = WindowManager.DefaultDisplay.Rotation;
                return rotation == SurfaceOrientation.Rotation0 || rotation == SurfaceOrientation.Rotation180;
            }
        }

        Rectangle _area;
        void SetSurfaceTransform(SurfaceTexture st, int width, int height)
        {
            var p = PreviewSize;
            if (p == null)
                return; // camera no ready yet, we will be called again later from SetupCamera.

            using (var metrics = new DisplayMetrics())
            {
                #region transform
                // Compensate for non-square pixels
                WindowManager.DefaultDisplay.GetMetrics(metrics);
                var aspectRatio = metrics.Xdpi / metrics.Ydpi; // close to 1, but rarely perfect 1

                // Compensate for preview streams aspect ratio
                aspectRatio *= (float)p.Height / p.Width;

                // Compensate for portrait mode
                if (IsPortrait)
                    aspectRatio = 1f / aspectRatio;

                // OpenGL coordinate system goes form 0 to 1
                var transform = new Matrix();
                transform.SetScale(1f, aspectRatio * width / height); // lock on to width

                Post(() =>
                {
                    try
                    {
                        SetTransform(transform);
                    }
                    catch (ObjectDisposedException) { } // todo: What to do here?! For now we squash :-/
                }); // ensure we use the right thread when updating transform

                Log.Debug(MobileBarcodeScanner.TAG, $"Aspect ratio: {aspectRatio}, Transform: {transform}");

                #endregion

                #region area
                using (var max = new RectF(0, 0, p.Width, p.Height))
                using (var r = new RectF(max))
                {
                    // Calculate area of interest within preview
                    var inverse = new Matrix();
                    transform.Invert(inverse);

                    Log.Debug(MobileBarcodeScanner.TAG, $"Inverse: {inverse}");

                    var flip = IsPortrait;
                    if (flip) r.Flip();
                    inverse.MapRect(r);
                    if (flip) r.Flip();

                    r.Intersect(max); // stream doesn't always fill the view!

                    // Compensate for reverse mounted camera, like on the Nexus 5X.
                    var reverse = _cameraInfo.Orientation == 270;
                    if (reverse)
                    {
                        if (flip)
                            r.OffsetTo(p.Width - r.Right, 0); // shift area right
                        else
                            r.Offset(0, p.Height - r.Bottom); // shift are down
                    }

                    _area = new Rectangle((int)r.Left, (int)r.Top, (int)r.Width(), (int)r.Height());

                    Log.Debug(MobileBarcodeScanner.TAG, $"Area: {_area}");
                }
                #endregion
            }
        }

        IWindowManager _wm;
        IWindowManager WindowManager
        {
            get
            {
                _wm = _wm ?? Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
                return _wm;
            }
        }

        bool? _hasTorch;
        public bool HasTorch
        {
            get
            {
                if (_hasTorch.HasValue)
                    return _hasTorch.Value;

                var p = _camera.GetParameters();
                var supportedFlashModes = p.SupportedFlashModes;

                if (supportedFlashModes != null
                    && (supportedFlashModes.Contains(Camera.Parameters.FlashModeTorch)
                    || supportedFlashModes.Contains(Camera.Parameters.FlashModeOn)))
                    _hasTorch = CheckTorchPermissions(false);

                return _hasTorch.HasValue && _hasTorch.Value;
            }
        }

        bool _isAnalyzing;
        public bool IsAnalyzing
        {
            get { return _isAnalyzing; }
        }

        bool _isTorchOn;
        public bool IsTorchOn
        {
            get { return _isTorchOn; }
        }

        MobileBarcodeScanningOptions _scanningOptions;
        IBarcodeReaderGeneric<FastJavaByteArray> _barcodeReader;
        public MobileBarcodeScanningOptions ScanningOptions
        {
            get { return _scanningOptions; }
            set
            {
                _scanningOptions = value;
                _barcodeReader = CreateBarcodeReader(value);
            }
        }

        bool _useContinuousFocus;
        bool _autoFocusRunning;
        public void AutoFocus()
        {
            _handler.PostSafe(() =>
            {
                var camera = _camera;
                if (camera == null || _autoFocusRunning || _useContinuousFocus)
                    return; // Allow camera to complete autofocus cycle, before trying again!

                _autoFocusRunning = true;
                camera.CancelAutoFocus();
                camera.AutoFocus(this);
            });
        }

        public void AutoFocus(int x, int y)
        {
            // todo: Needs some slightly serious math to map back to camera coordinates. 
            // The method used in ZXingSurfaceView is simply wrong.
            AutoFocus();
        }

        public void OnAutoFocus(bool focus, Camera camera)
        {
            Log.Debug(MobileBarcodeScanner.TAG, $"OnAutoFocus: {focus}");
            _autoFocusRunning = false;
            if (!(focus || _useContinuousFocus))
                AutoFocus();
        }

        public void PauseAnalysis()
        {
            _isAnalyzing = false;
        }

        public void ResumeAnalysis()
        {
            _isAnalyzing = true;
        }

        Action<Result> _callback;
        public void StartScanning(Action<Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
        {
            _callback = scanResultCallback;
            ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

            _handler.PostSafe(SetupCamera);

            ResumeAnalysis();
        }

        void OpenCamera()
        {
            if (_camera != null)
                return;

            CheckCameraPermissions();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread) // Choose among multiple cameras from Gingerbread forward
            {
                int max = Camera.NumberOfCameras;
                Log.Debug(MobileBarcodeScanner.TAG, $"Found {max} cameras");
                var requestedFacing = CameraFacing.Back; // default to back facing camera, ..
                if (ScanningOptions.UseFrontCameraIfAvailable.HasValue && ScanningOptions.UseFrontCameraIfAvailable.Value)
                    requestedFacing = CameraFacing.Front; // .. but use front facing if available and requested

                var info = new Camera.CameraInfo();
                int idx = 0;
                do
                {
                    Camera.GetCameraInfo(idx++, info); // once again Android sucks!
                }
                while (info.Facing != requestedFacing && idx < max);
                --idx;

                Log.Debug(MobileBarcodeScanner.TAG, $"Opening {info.Facing} facing camera: {idx}...");
                _cameraInfo = info;
                _camera = Camera.Open(idx);
            }
            else
            {
                _camera = Camera.Open();
            }

            _camera.Lock();
        }

        async Task SetupCamera()
        {
            OpenCamera();

            var p = _camera.GetParameters();
            p.PreviewFormat = ImageFormatType.Nv21; // YCrCb format (all Android devices must support this)

            // Android actually defines a barcode scene mode
            if (p.SupportedSceneModes.Contains(Camera.Parameters.SceneModeBarcode)) // we might be lucky :-)
                p.SceneMode = Camera.Parameters.SceneModeBarcode;

            // First try continuous video, then auto focus, then fixed
            var supportedFocusModes = p.SupportedFocusModes;
            if (supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousVideo))
                p.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
                p.FocusMode = Camera.Parameters.FocusModeAuto;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeFixed))
                p.FocusMode = Camera.Parameters.FocusModeFixed;

            // Set automatic white balance if possible
            if (p.SupportedWhiteBalance.Contains(Camera.Parameters.WhiteBalanceAuto))
                p.WhiteBalance = Camera.Parameters.WhiteBalanceAuto;

            // Check if we can support requested resolution ..
            var availableResolutions = p.SupportedPreviewSizes.Select(s => new CameraResolution { Width = s.Width, Height = s.Height }).ToList();
            var resolution = ScanningOptions.GetResolution(availableResolutions);

            // .. If not, let's try and find a suitable one
            resolution = resolution ?? availableResolutions.OrderBy(r => r.Width).FirstOrDefault(r => r.Width.Between(640, 1280) && r.Height.Between(640, 960));

            // Hopefully a resolution was selected at some point
            if (resolution != null)
                p.SetPreviewSize(resolution.Width, resolution.Height);

            _camera.SetParameters(p);

            SetupTorch(_isTorchOn);

            p = _camera.GetParameters(); // refresh!

            _useContinuousFocus = p.FocusMode == Camera.Parameters.FocusModeContinuousVideo;
            PreviewSize = p.PreviewSize; // get actual preview size (may differ from requested size)
            var bitsPerPixel = ImageFormat.GetBitsPerPixel(p.PreviewFormat);

            Log.Debug(MobileBarcodeScanner.TAG, $"Preview size {PreviewSize.Width}x{PreviewSize.Height} with {bitsPerPixel} bits per pixel");

            var surfaceInfo = await _surfaceAvailable.Task;

            SetSurfaceTransform(surfaceInfo.Surface, surfaceInfo.Width, surfaceInfo.Height);

            _camera.SetDisplayOrientation(CameraOrientation(WindowManager.DefaultDisplay.Rotation));
            _camera.SetPreviewTexture(surfaceInfo.Surface);
            _camera.StartPreview();

            int bufferSize = (PreviewSize.Width * PreviewSize.Height * bitsPerPixel) / 8;
            using (var buffer = new FastJavaByteArray(bufferSize))
                _camera.AddCallbackBuffer(buffer);

            _camera.SetNonMarshalingPreviewCallback(this);

            // Docs suggest if Auto or Macro modes, we should invoke AutoFocus at least once
            _autoFocusRunning = false;
            if (!_useContinuousFocus)
                AutoFocus();
        }

        public int CameraOrientation(SurfaceOrientation rotation)
        {
            int degrees = 0;
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
            }

            // Handle front facing camera
            if (_cameraInfo.Facing == CameraFacing.Front)
                return (360 - ((_cameraInfo.Orientation + degrees) % 360)) % 360;  // compensate for mirror

            return (_cameraInfo.Orientation - degrees + 360) % 360;
        }

        void ShutdownCamera()
        {
            _handler.Post(() =>
            {
                if (_camera == null)
                    return;

                var camera = _camera;
                _camera = null;

                try
                {
                    camera.StopPreview();
                    camera.SetNonMarshalingPreviewCallback(null);
                }
                catch (Exception e)
                {
                    Log.Error(MobileBarcodeScanner.TAG, e.ToString());
                }
                finally
                {
                    camera.Release();
                }
            });
        }

        public void StopScanning()
        {
            PauseAnalysis();
            ShutdownCamera();
        }

        public void Torch(bool on)
        {
            if (!Context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFlash))
            {
                Log.Info(MobileBarcodeScanner.TAG, "Flash not supported on this device");
                return;
            }

            CheckTorchPermissions();

            _isTorchOn = on;
            if (_camera != null) // already running
                SetupTorch(on);
        }

        public void ToggleTorch()
        {
            Torch(!_isTorchOn);
        }

        void SetupTorch(bool on)
        {
            var p = _camera.GetParameters();
            var supportedFlashModes = p.SupportedFlashModes ?? Enumerable.Empty<string>();

            string flashMode = null;

            if (on)
            {
                if (supportedFlashModes.Contains(Camera.Parameters.FlashModeTorch))
                    flashMode = Camera.Parameters.FlashModeTorch;
                else if (supportedFlashModes.Contains(Camera.Parameters.FlashModeOn))
                    flashMode = Camera.Parameters.FlashModeOn;
            }
            else
            {
                if (supportedFlashModes.Contains(Camera.Parameters.FlashModeOff))
                    flashMode = Camera.Parameters.FlashModeOff;
            }

            if (!string.IsNullOrEmpty(flashMode))
            {
                p.FlashMode = flashMode;
                _camera.SetParameters(p);
            }
        }

        bool CheckCameraPermissions(bool throwOnError = true)
        {
            return CheckPermissions(Android.Manifest.Permission.Camera, throwOnError);
        }

        bool CheckTorchPermissions(bool throwOnError = true)
        {
            return CheckPermissions(Android.Manifest.Permission.Flashlight, throwOnError);
        }

        bool CheckPermissions(string permission, bool throwOnError = true)
        {
            Log.Debug(MobileBarcodeScanner.TAG, $"Checking {permission}...");

            if (!PermissionsHandler.IsPermissionInManifest(Context, permission)
                || !PermissionsHandler.IsPermissionGranted(Context, permission))
            {
                var msg = $"Requires: {permission}, but was not found in your AndroidManifest.xml file.";
                Log.Error(MobileBarcodeScanner.TAG, msg);

                if (throwOnError)
                    throw new UnauthorizedAccessException(msg);

                return false;
            }

            return true;
        }

        IBarcodeReaderGeneric<FastJavaByteArray> CreateBarcodeReader(MobileBarcodeScanningOptions options)
        {
            var barcodeReader = new BarcodeReaderGeneric<FastJavaByteArray>();

            if (options == null)
                return barcodeReader;

            if (options.TryHarder.HasValue)
                barcodeReader.Options.TryHarder = options.TryHarder.Value;

            if (options.PureBarcode.HasValue)
                barcodeReader.Options.PureBarcode = options.PureBarcode.Value;

            if (!string.IsNullOrEmpty(options.CharacterSet))
                barcodeReader.Options.CharacterSet = options.CharacterSet;

            if (options.TryInverted.HasValue)
                barcodeReader.TryInverted = options.TryInverted.Value;

            if (options.AutoRotate.HasValue)
                barcodeReader.AutoRotate = options.AutoRotate.Value;

            if (options.PossibleFormats?.Any() ?? false)
            {
                barcodeReader.Options.PossibleFormats = new List<BarcodeFormat>();

                foreach (var pf in options.PossibleFormats)
                    barcodeReader.Options.PossibleFormats.Add(pf);
            }

            return barcodeReader;
        }

        protected override void Dispose(bool disposing)
        {
            _orientationEventListener?.Disable();
            _orientationEventListener?.Dispose();
            _orientationEventListener = null;
            base.Dispose(disposing);
        }

		private bool _wasScanned;
		private DateTime _lastPreviewAnalysis;
		private bool CanAnalyzeFrame
		{
			get
			{
				if (!IsAnalyzing)
					return false;

				var elapsedTimeMs = (DateTime.UtcNow - _lastPreviewAnalysis).TotalMilliseconds;
				if (elapsedTimeMs < _scanningOptions.DelayBetweenAnalyzingFrames)
					return false;

				// Delay a minimum between scans
				if (_wasScanned && elapsedTimeMs < _scanningOptions.DelayBetweenContinuousScans)
					return false;

				// reset!
				_wasScanned = false;
				_lastPreviewAnalysis = DateTime.UtcNow;

				return true;
			}
		}

		byte[] _buffer;
        async public void OnPreviewFrame(IntPtr data, Camera camera)
        {
            System.Diagnostics.Stopwatch sw = null;
            using (var fastArray = new FastJavaByteArray(data)) // avoids marshalling
            {
                try
                {
#if DEBUG
                    sw = new Stopwatch();
                    sw.Start();
#endif
					if (!CanAnalyzeFrame)
                        return;

                    var isPortrait = IsPortrait; // this is checked asynchronously, so make sure to copy.

                    var result = await Task.Run(() =>
                    {
                        var dataWidth = PreviewSize.Width;
                        var dataHeight = PreviewSize.Height;

                        LuminanceSource luminanceSource;
                        if (isPortrait)
                        {
                            fastArray.Transpose(ref _buffer, dataWidth, dataHeight);
                            luminanceSource = new FastJavaByteArrayYUVLuminanceSource(fastArray, dataHeight, dataWidth, _area.Top, _area.Left, _area.Height, _area.Width);
                        }
                        else
                            luminanceSource = new FastJavaByteArrayYUVLuminanceSource(fastArray, dataWidth, dataHeight, _area.Left, _area.Top, _area.Width, _area.Height);

                        return _barcodeReader.Decode(luminanceSource);
                    });

					if (result != null)
					{
						_wasScanned = true;
						_callback(result);
					}
                    else if (!_useContinuousFocus)
                        AutoFocus();
                }
                catch (Exception ex)
                {
                    // It is better to just skip a frame :-) ..
                    Log.Warn(MobileBarcodeScanner.TAG, ex.ToString());
                }
                finally
                {
                    camera.AddCallbackBuffer(fastArray); // IMPORTANT!

#if DEBUG
                    sw.Stop();
                    try
                    {
                        Post(() =>
                        {
                            _toast.SetText(string.Format("{0}ms", sw.ElapsedMilliseconds));
                            _toast.Show();
                        });
                    }
                    catch { } // squash
#endif
                }
            }
        }
    }
}
