using System;
using System.Threading.Tasks;
using Android.Views;
using ApxLabs.FastAndroidCamera;

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
			cameraController.SetupCamera();
			barcodeReader = scannerHost.ScanningOptions.BuildBarcodeReader();
		}

		public void AutoFocus()
			=> cameraController.AutoFocus();

		public void AutoFocus(int x, int y)
			=> cameraController.AutoFocus(x, y);

		public void RefreshCamera()
			=> cameraController.RefreshCamera();

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

		void HandleOnPreviewFrameReady(object sender, FastJavaByteArray fastArray)
		{
			if (!CanAnalyzeFrame)
				return;

			wasScanned = false;
			lastPreviewAnalysis = DateTime.UtcNow;

			processingTask = Task.Run(() =>
			{
				try
				{
					DecodeFrame(fastArray);
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

		void DecodeFrame(FastJavaByteArray fastArray)
		{
			var resolution = cameraController.CameraResolution;
			var width = resolution.Width;
			var height = resolution.Height;

			var rotate = false;
			var newWidth = width;
			var newHeight = height;

			// use last value for performance gain
			var cDegrees = cameraController.LastCameraDisplayOrientationDegree;

			if (cDegrees == 90 || cDegrees == 270)
			{
				rotate = true;
				newWidth = height;
				newHeight = width;
			}

			var start = PerformanceCounter.Start();

			LuminanceSource fast = new FastJavaByteArrayYUVLuminanceSource(fastArray, width, height, 0, 0, width, height); // _area.Left, _area.Top, _area.Width, _area.Height);
			if (rotate)
				fast = fast.rotateCounterClockwise();

			var result = barcodeReader.Decode(fast);

			fastArray.Dispose();
			fastArray = null;

			PerformanceCounter.Stop(start,
				"Decode Time: {0} ms (width: " + width + ", height: " + height + ", degrees: " + cDegrees + ", rotate: " +
				rotate + ")");

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