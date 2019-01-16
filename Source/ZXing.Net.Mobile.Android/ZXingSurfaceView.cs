using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using ZXing.Mobile.CameraAccess;

namespace ZXing.Mobile
{
    public class ZXingSurfaceView : SurfaceView, ISurfaceHolderCallback, IScannerView, IScannerSessionHost
    {
        public ZXingSurfaceView(Context context, MobileBarcodeScanningOptions options)
            : base(context)
        {
            ScanningOptions = options ?? new MobileBarcodeScanningOptions();
            Init();
        }

        protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Init();
        }

		bool addedHolderCallback = false;

        private void Init()
        {
			if (_cameraAnalyzer == null)
	            _cameraAnalyzer = new CameraAnalyzer(this, this);

			_cameraAnalyzer.ResumeAnalysis();

			if (!addedHolderCallback) {
				Holder.AddCallback(this);
				Holder.SetType(SurfaceType.PushBuffers);
				addedHolderCallback = true;
			}
        }

        public async void SurfaceCreated(ISurfaceHolder holder)
        {
            //avoid duplicate setups, forcing the camera to be setup even when the camera was not scanning (this can happen when resuming the app)
            if (!_surfaceCreated)
            {
                await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;
                _cameraAnalyzer.SetupCamera();
                _surfaceCreated = true;
            }
        }

        public async void SurfaceChanged(ISurfaceHolder holder, Format format, int wx, int hx)
        {
            await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;

            _cameraAnalyzer.RefreshCamera();
        }

        public async void SurfaceDestroyed(ISurfaceHolder holder)
        {
            await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;

            try {
				if (addedHolderCallback) {
					Holder.RemoveCallback(this);
					addedHolderCallback = false;
				}
            } catch { }

            _cameraAnalyzer.ShutdownCamera();
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            var r = base.OnTouchEvent(e);

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    return true;
                case MotionEventActions.Up:
                    var touchX = e.GetX();
                    var touchY = e.GetY();
                    this.AutoFocus((int)touchX, (int)touchY);
                    break;
            }

            return r;
        }

        public void AutoFocus()
        {
            _cameraAnalyzer.AutoFocus();
        }

        public void AutoFocus(int x, int y)
        {
            _cameraAnalyzer.AutoFocus(x, y);
        }

        public void StartScanning(Action<Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
        {
            //fix Android 7 bug: camera freezes because surfacedestroyed function isn't always called correct, the old surfaceview was still visible.
            this.Visibility = ViewStates.Visible;

            //make sure the camera is setup
            _cameraAnalyzer.SetupCamera();

            ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

            _cameraAnalyzer.BarcodeFound += (sender, result) =>
            {
                scanResultCallback?.Invoke(result);
            };
            _cameraAnalyzer.ResumeAnalysis();
        }

        public void StopScanning()
        {
            _cameraAnalyzer.ShutdownCamera();
            //fix Android 7 bug: camera freezes because surfacedestroyed function isn't always called correct, the old surfaceview was still visible.
            this.Visibility = ViewStates.Gone;
        }

        public void PauseAnalysis()
        {
            _cameraAnalyzer.PauseAnalysis();
        }

        public void ResumeAnalysis()
        {
            _cameraAnalyzer.ResumeAnalysis();
        }

        public void Torch(bool on)
        {
            if (on)
                _cameraAnalyzer.Torch.TurnOn();
            else
                _cameraAnalyzer.Torch.TurnOff();
        }

        public void ToggleTorch()
        {
            _cameraAnalyzer.Torch.Toggle();
        }

        public MobileBarcodeScanningOptions ScanningOptions { get; set; }

        public bool IsTorchOn => _cameraAnalyzer.Torch.IsEnabled;

        public bool IsAnalyzing => _cameraAnalyzer.IsAnalyzing;

        private CameraAnalyzer _cameraAnalyzer;
        private bool _surfaceCreated;

        public bool HasTorch => _cameraAnalyzer.Torch.IsSupported;


        #region possibl future drawing code

        //        private void drawResultPoints (Bitmap barcode, ZXing.Result rawResult)
        //        {
        //            var points = rawResult.ResultPoints;
        //          
        //            if (points != null && points.Length > 0) {
        //                var canvas = new Canvas (barcode);
        //                Paint paint = new Paint ();
        //                paint.Color = Android.Graphics.Color.White;
        //                paint.StrokeWidth = 3.0f;
        //                paint.SetStyle (Paint.Style.Stroke);
        //              
        //                var border = new RectF (2, 2, barcode.Width - 2, barcode.Height - 2);
        //                canvas.DrawRect (border, paint);
        //              
        //                paint.Color = Android.Graphics.Color.Purple;
        //              
        //                if (points.Length == 2) {
        //                    paint.StrokeWidth = 4.0f;
        //                    drawLine (canvas, paint, points [0], points [1]);
        //                } else if (points.Length == 4 &&
        //                (rawResult.BarcodeFormat == BarcodeFormat.UPC_A ||
        //                rawResult.BarcodeFormat == BarcodeFormat.EAN_13)) {
        //                    // Hacky special case -- draw two lines, for the barcode and metadata
        //                    drawLine (canvas, paint, points [0], points [1]);
        //                    drawLine (canvas, paint, points [2], points [3]);
        //                } else {
        //                    paint.StrokeWidth = 10.0f;
        //                  
        //                    foreach (ResultPoint point in points)
        //                        canvas.DrawPoint (point.X, point.Y, paint);
        //                }
        //            }
        //        }

        //        private void drawLine (Canvas canvas, Paint paint, ResultPoint a, ResultPoint b)
        //        {
        //            canvas.DrawLine (a.X, a.Y, b.X, b.Y, paint);
        //        }

        #endregion

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			// Reinit things
			Init();
		}

		protected override void OnWindowVisibilityChanged(ViewStates visibility)
		{
			base.OnWindowVisibilityChanged(visibility);
			if (visibility == ViewStates.Visible)
				Init();
		}

        public override async void OnWindowFocusChanged(bool hasWindowFocus)
        {
            base.OnWindowFocusChanged(hasWindowFocus);
            
            if (!hasWindowFocus) return;
            // SurfaceCreated/SurfaceChanged are not called on a resume
            await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;

            //only refresh the camera if the surface has already been created. Fixed #569
            if (_surfaceCreated)
                _cameraAnalyzer.RefreshCamera();
        }
    }
}
