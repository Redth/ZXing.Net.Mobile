using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using ZXing.Mobile.CameraAccess;

namespace ZXing.Mobile
{
    public class ZXingSurfaceView : SurfaceView, ISurfaceHolderCallback, IScannerView
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

        private void Init()
        {
            _cameraAnalyzer = new CameraAnalyzer(this, ScanningOptions);
            Holder.AddCallback(this);
            Holder.SetType(SurfaceType.PushBuffers);
        }

        public async void SurfaceCreated(ISurfaceHolder holder)
        {
            await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;

            _cameraAnalyzer.SetupCamera();
        }

        public async void SurfaceChanged(ISurfaceHolder holder, Format format, int wx, int hx)
        {
            await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;

            _cameraAnalyzer.RefreshCamera();
        }

        public async void SurfaceDestroyed(ISurfaceHolder holder)
        {
            await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;

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

        public MobileBarcodeScanningOptions ScanningOptions { get; private set; }

        public bool IsTorchOn => _cameraAnalyzer.Torch.IsEnabled;

        public bool IsAnalyzing => _cameraAnalyzer.IsAnalyzing;

        private CameraAnalyzer _cameraAnalyzer;

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
    }
}
