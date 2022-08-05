using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Views;
using ZXing.Net.Mobile.Android;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraAnalyzer
    {
        readonly Context context;
        readonly CameraController cameraController;
        readonly CameraEventsListener cameraEventListener;
        readonly DeviceOrientationEventListener orientationEventListener;
        Task processingTask;
        DateTime lastPreviewAnalysis = DateTime.UtcNow;
        bool wasScanned;
        readonly IScannerSessionHost scannerHost;
        BarcodeReaderGeneric barcodeReader;

        public CameraAnalyzer(SurfaceView surfaceView, IScannerSessionHost scannerHost)
        {
            context = surfaceView.Context;
            this.scannerHost = scannerHost;
            cameraEventListener = new CameraEventsListener();
            orientationEventListener = new DeviceOrientationEventListener(context, Android.Hardware.SensorDelay.Normal);
            cameraController = new CameraController(surfaceView, cameraEventListener, scannerHost);
            Torch = new Torch(cameraController, surfaceView.Context);
        }

        public Action<Result> BarcodeFound;

        public Torch Torch { get; }

        public bool IsAnalyzing { get; private set; }

        public void PauseAnalysis()
            => IsAnalyzing = false;

        public void ResumeAnalysis()
            => IsAnalyzing = true;

        public void ShutdownCamera()
        {
            IsAnalyzing = false;
            cameraEventListener.OnPreviewFrameReady -= HandleOnPreviewFrameReady;
            orientationEventListener.Disable();
            cameraController.ShutdownCamera();
        }

        public void SetupCamera()
        {
            cameraEventListener.OnPreviewFrameReady += HandleOnPreviewFrameReady;
            if (orientationEventListener.CanDetectOrientation())
            {
                orientationEventListener.Enable();
            }

            barcodeReader = scannerHost.ScanningOptions.BuildBarcodeReader();
            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Created Barcode Reader");

            cameraController.SetupCamera();
        }

        public void AutoFocus()
            => cameraController.AutoFocus();

        public void AutoFocus(int x, int y)
            => cameraController.AutoFocus(x, y);

        public void RefreshCamera()
        {
            cameraController.RefreshCamera();
        }

        bool CanAnalyzeFrame
        {
            get
            {
                if (!IsAnalyzing)
                    return false;

                //Check and see if we're still processing a previous frame
                // todo: check if we can run as many as possible or mby run two analyzers at once (Vision + ZXing)
                if (processingTask != null && !processingTask.IsCompleted)
                    return false;

                var elapsedTimeMs = (DateTime.UtcNow - lastPreviewAnalysis).TotalMilliseconds;
                if (elapsedTimeMs < scannerHost.ScanningOptions.DelayBetweenAnalyzingFrames)
                    return false;

                // Delay a minimum between scans
                if (wasScanned && elapsedTimeMs < scannerHost.ScanningOptions.DelayBetweenContinuousScans)
                    return false;

                return true;
            }
        }

        void HandleOnPreviewFrameReady(object sender, CapturedImageData data)
        {
            if (!CanAnalyzeFrame)
                return;

            wasScanned = false;
            lastPreviewAnalysis = DateTime.UtcNow;

            processingTask = Task.Run(() =>
            {
                try
                {
                    DecodeFrame(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "DecodeFrame exception occurs");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        void DecodeFrame(CapturedImageData data)
        {
            var start = PerformanceCounter.Start();
            var orientationData = new DeviceOrientationData(context.Resources.Configuration.Orientation, orientationEventListener.Orientation, cameraController.SensorRotation);
            var source = new PlanarNV21LuminanceSource(data.Matrix, data.Width, data.Height, orientationData, (!barcodeReader.AutoRotate && orientationEventListener.IsEnabled));
            var initPerformance = PerformanceCounter.Stop(start);

            //DebugHelper.SendNV21toJPEGToEndpoint(source.Matrix, source.Width, source.Height, "https://local.imagereceiver.ip:5390");

            start = PerformanceCounter.Start();
            var result = barcodeReader.Decode(source);
            Android.Util.Log.Debug(
                MobileBarcodeScanner.TAG,
                "Decode Time: {0} ms (Width: {1}, Height: {2}, Rotations (S/D): {3} / {4}), Source setup: {5} ms",
                PerformanceCounter.Stop(start).Milliseconds,
                data.Width,
                data.Height,
                orientationData.SensorRotation,
                orientationData.DeviceOrientation,
                initPerformance.Milliseconds);

            if (result != null)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Barcode Found");

                wasScanned = true;
                BarcodeFound?.Invoke(result);
                return;
            }
        }
    }
}
