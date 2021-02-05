using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Views;
using ZXing.Common;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraAnalyzer
    {
        readonly CameraController cameraController;
        readonly CameraEventsListener cameraEventListener;
        Task processingTask;
        DateTime lastPreviewAnalysis = DateTime.UtcNow;
        bool wasScanned;
        readonly IScannerSessionHost scannerHost;
        BarcodeReaderGeneric barcodeReader;

        public CameraAnalyzer(SurfaceView surfaceView, IScannerSessionHost scannerHost)
        {
            this.scannerHost = scannerHost;
            cameraEventListener = new CameraEventsListener();
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
            cameraController.ShutdownCamera();
        }

        public void SetupCamera()
        {
            cameraEventListener.OnPreviewFrameReady += HandleOnPreviewFrameReady;

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

        void HandleOnPreviewFrameReady(object sender, byte[] data)
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

        void DecodeFrame(byte[] data)
        {
            var previewSize = cameraController.IdealPhotoSize;
            var width = previewSize.Width;
            var height = previewSize.Height;
            
            var start = PerformanceCounter.Start();

            barcodeReader.AutoRotate = true;

            var source = new PlanarYUVLuminanceSource(data, width, height, 0, 0, width, height, false);

            var result = barcodeReader.Decode(source);
            PerformanceCounter.Stop(start,
                "Decode Time: {0} ms (width: " + width + ", height: " + height + ")");

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
