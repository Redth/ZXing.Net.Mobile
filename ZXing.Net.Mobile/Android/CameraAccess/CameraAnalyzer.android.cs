using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Android.Views;
using ApxLabs.FastAndroidCamera;

namespace ZXing.UI
{
	internal class CameraAnalyzer
	{
		readonly CameraController cameraController;
		readonly CameraEventsListener cameraEventListener;
		Task processingTask;
		DateTime lastPreviewAnalysis = DateTime.UtcNow;
		bool wasScanned;
		
		public Action<ZXing.Result[]> ResultHandler { get; }
		public BarcodeScanningOptions Options { get; }


		public CameraAnalyzer(SurfaceView surfaceView, BarcodeScanningOptions options, Action<ZXing.Result[]> resultHandler)
		{
			Options = options;

			ResultHandler = resultHandler;
			cameraEventListener = new CameraEventsListener();
			cameraController = new CameraController(surfaceView, cameraEventListener, options);
			Torch = new Torch(cameraController, surfaceView.Context);
		}

		public Torch Torch { get; }

		public bool IsAnalyzing { get; set; }

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
		}

		public Task AutoFocusAsync()
			=> cameraController.AutoFocusAsync();

		public Task AutoFocusAsync(int x, int y)
			=> cameraController.AutoFocusAsync(x, y);

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
				if (elapsedTimeMs < Options.DelayBetweenAnalyzingFrames)
					return false;

				// Delay a minimum between scans
				if (wasScanned && elapsedTimeMs < Options.DelayBetweenContinuousScans)
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
					Logger.Error(ex);
				}
			}).ContinueWith(task =>
			{
				if (task.IsFaulted)
					Logger.Error(task.Exception, "DecodeFrame exception occurred");
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		void DecodeFrame(FastJavaByteArray fastArray)
		{
			var cameraParameters = cameraController.Camera.GetParameters();
			var width = cameraParameters.PreviewSize.Width;
			var height = cameraParameters.PreviewSize.Height;

			var barcodeReader = Options.BuildBarcodeReader();

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

			ZXing.Result[] results = null;
			var start = PerformanceCounter.Start();

			LuminanceSource fast = new FastJavaByteArrayYUVLuminanceSource(fastArray, width, height, 0, 0, width, height); // _area.Left, _area.Top, _area.Width, _area.Height);
			if (rotate)
				fast = fast.rotateCounterClockwise();

			if (Options.ScanMultiple)
				results = barcodeReader.DecodeMultiple(fast);
			else
				results = new[] { barcodeReader.Decode(fast) };
			

			fastArray.Dispose();
			fastArray = null;

			PerformanceCounter.Stop(start,
				"Decode Time: {0} ms (width: " + width + ", height: " + height + ", degrees: " + cDegrees + ", rotate: " +
				rotate + ")");

			if (results != null && results.Length > 0 && results[0] != null)
			{
				Logger.Info("Barcode Found");

				wasScanned = true;
				ResultHandler?.Invoke(results);
				return;
			}
		}
	}
}