using System;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using Xamarin.Essentials;

namespace ZXing.Mobile.CameraAccess
{
	public class CameraAnalyzer
	{
		readonly CameraController cameraController;
		readonly CameraEventsListener cameraEventListener;
		readonly ZXingSurfaceView surfaceView;
		Task processingTask;
		DateTime lastPreviewAnalysis = DateTime.UtcNow;
		bool wasScanned;
		IScannerSessionHost scannerHost;        

		//used for decoding
		//updated only when the device is rotated or the CustomOverlayScanAreaView (if it is used) is changed
		int widthCamera;
		int heightCamera;
		int widthScanAreaScaled;
		int heightScanAreaScaled;
		int offsetScanAreaLeftPixelsScaled;
		int offsetScanAreaTopPixelsScaled;
		int? lastDecodedCameraDisplayRotationDegree = null;
		bool needRecalculateDecodingVariables = false;

		enum Dimension
		{
			Width,
			Height
		};

		public CameraAnalyzer(SurfaceView surfaceView, IScannerSessionHost scannerHost)
		{
			if (surfaceView is ZXingSurfaceView)
                this.surfaceView = surfaceView as ZXingSurfaceView;

			this.scannerHost = scannerHost;
			cameraEventListener = new CameraEventsListener();
			cameraController = new CameraController(surfaceView, cameraEventListener, scannerHost);
			Torch = new Torch(cameraController, surfaceView.Context);
			lastDecodedCameraDisplayRotationDegree = null;
			if (this.surfaceView?.CustomScanArea != null)
			{
                this.surfaceView.CustomOverlay.LayoutChange += CustomOverlayScanAreaView_LayoutChange;
			}
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

        void CustomOverlayScanAreaView_LayoutChange(object sender, View.LayoutChangeEventArgs e)
		{
			needRecalculateDecodingVariables = true;
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

		void SetCameraWidthHeightVariables()
		{
			var cameraParameters = cameraController.Camera.GetParameters();

			if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
			{
				//the Android.Hardware.Camera API thinks that portrait is rotated
				widthCamera = cameraParameters.PreviewSize.Height;
				heightCamera = cameraParameters.PreviewSize.Width;
			}
			else
			{
				widthCamera = cameraParameters.PreviewSize.Width;
				heightCamera = cameraParameters.PreviewSize.Height;
			}
		}

		void SetDecodeVariablesForWholeScreen()
		{
			SetCameraWidthHeightVariables();
			offsetScanAreaLeftPixelsScaled = 0;
			offsetScanAreaTopPixelsScaled = 0;
			heightScanAreaScaled = heightCamera;
			widthScanAreaScaled = widthCamera;
		}

		//thank you so much Dr.Math for providing this formula
		//http://mathforum.org/library/drmath/view/60433.html
		int ScaleSurfaceViewToPreviewSize(int value, Rect surfaceView, Dimension dim)
		{
			double surfaceA = 0;
			double surfaceB = 0;
			double previewA = 0;
			double previewB = 0;

			switch (dim)
			{
				case Dimension.Height:
					surfaceA = 0;
					surfaceB = surfaceView.Bottom;
					previewA = 0;
					previewB = heightCamera;
					break;
				case Dimension.Width:
					surfaceA = 0;
					surfaceB = surfaceView.Right;
					previewA = 0;
					previewB = widthCamera;
					break;
			}

			var previewDiff = previewB - previewA;
			var surfaceDiff = surfaceB - surfaceA;
			var prevToSurfaceDiffRatio = previewDiff / surfaceDiff;
			var surfaceLowDiff = value - surfaceA;

			return (int)(previewA + (surfaceLowDiff * prevToSurfaceDiffRatio));
		}

		//thank you so much Dr.Math for providing this formula
		//http://mathforum.org/library/drmath/view/60433.html
		int ScaleSurfaceViewToPreviewLocation(int value, Rect surfaceView, Dimension dim)
		{
			double surfaceA = 0;
			double surfaceB = 0;
			double previewA = 0;
			double previewB = 0;

			switch (dim)
			{
				case Dimension.Height:
					surfaceA = surfaceView.Top;
					surfaceB = surfaceView.Bottom;
					previewA = 0;
					previewB = heightCamera;
					break;
				case Dimension.Width:
					surfaceA = surfaceView.Left;
					surfaceB = surfaceView.Right;
					previewA = 0;
					previewB = widthCamera;
					break;
			}

			var previewDiff = previewB - previewA;
			var surfaceDiff = surfaceB - surfaceA;
			var prevToSurfaceDiffRatio = previewDiff / surfaceDiff;
			var surfaceLowDiff = value - surfaceA;

			return (int)(previewA + (surfaceLowDiff * prevToSurfaceDiffRatio));
		}

		void SetDecodeVariablesForCustomScanArea()
		{
			SetCameraWidthHeightVariables();
			var rectangleScanArea = new Rect();
			surfaceView.CustomScanArea?.GetGlobalVisibleRect(rectangleScanArea);
			var originalWidth = rectangleScanArea.Right - rectangleScanArea.Left;
			var originalHeight = rectangleScanArea.Bottom - rectangleScanArea.Top;

			var rectangleSurfaceView = new Rect();
			surfaceView.GetGlobalVisibleRect(rectangleSurfaceView);

			//calculate the width and heigh of the scan area converting from the surface view resolution to the preview resolution
			widthScanAreaScaled = ScaleSurfaceViewToPreviewSize(originalWidth, rectangleSurfaceView, Dimension.Width);
			heightScanAreaScaled = ScaleSurfaceViewToPreviewSize(originalHeight, rectangleSurfaceView, Dimension.Height);

			//calculate the position of the scan area converting from the surface view resolution to the preview resolution
			var scanAreaTop = ScaleSurfaceViewToPreviewLocation(rectangleScanArea.Top, rectangleSurfaceView, Dimension.Height);
			var scanAreaLeft = ScaleSurfaceViewToPreviewLocation(rectangleScanArea.Left, rectangleSurfaceView, Dimension.Width);
			var scanAreaRight = ScaleSurfaceViewToPreviewLocation(rectangleScanArea.Right, rectangleSurfaceView, Dimension.Width);
			var scanAreaBottom = ScaleSurfaceViewToPreviewLocation(rectangleScanArea.Bottom, rectangleSurfaceView, Dimension.Height);

			//depending on the rotation angle of the camera the perspective of "Left" and "Top" is different
			//some of the calculations are different than you may expect.  Rotation0 for example using a Right offset for Left.
			//I believe some of them are backwards due to the camera preview rotation needing correction (SetCameraDisplayOrientation in CameraController.cs)           
			//or MUCH more likely the Android.Hardware.Camera API is from the Stranger Things upsidedown universe where the Demogorgon lives
			if (DeviceDisplay.MainDisplayInfo.Rotation == DisplayRotation.Rotation0)
			{
				offsetScanAreaLeftPixelsScaled = widthCamera - scanAreaRight;
				offsetScanAreaTopPixelsScaled = scanAreaTop;
			}
			else if (DeviceDisplay.MainDisplayInfo.Rotation == DisplayRotation.Rotation90)
			{
				offsetScanAreaLeftPixelsScaled = scanAreaLeft;
				offsetScanAreaTopPixelsScaled = scanAreaTop;
			}
			else if (DeviceDisplay.MainDisplayInfo.Rotation == DisplayRotation.Rotation180)
			{
				//not sure about these.  Devices don't want to rotate upside down.  But they should be opposite from a 0 degree rotation.
				offsetScanAreaLeftPixelsScaled = scanAreaLeft;
				offsetScanAreaTopPixelsScaled = heightCamera - scanAreaBottom;
			}
			else if (DeviceDisplay.MainDisplayInfo.Rotation == DisplayRotation.Rotation270)
			{
				offsetScanAreaLeftPixelsScaled = widthCamera - scanAreaRight;
				offsetScanAreaTopPixelsScaled = heightCamera - scanAreaBottom;
			}
			else
			{
				//something is wrong, just scan the whole available preview
				SetDecodeVariablesForWholeScreen();
			}
		}

		void DecodeFrame(FastJavaByteArray fastArray)
		{
			var rotate = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait;
			var start = PerformanceCounter.Start();

			//only calculate the variables needed for decoding if the camera is rotated or the CustomScanAreaView (if used) is changed
			if (lastDecodedCameraDisplayRotationDegree != cameraController.CurrentCameraDisplayRotationDegree || needRecalculateDecodingVariables)
			{
				if (surfaceView != null && surfaceView.UsingCustomScanArea)
					SetDecodeVariablesForCustomScanArea();
				else
					SetDecodeVariablesForWholeScreen();

				lastDecodedCameraDisplayRotationDegree = cameraController.CurrentCameraDisplayRotationDegree;
				needRecalculateDecodingVariables = false;
			}

			var barcodeReader = scannerHost.ScanningOptions.BuildBarcodeReader();
			LuminanceSource fast;

			if (rotate)
			{
				//for decoding it is expected that the FastJavaByteArrayYUVLuminanceSource will be in landscape orientation
				//if we are in portrait mode the array must be rotated
				//also since we are in portrait mode and all dimensions are calculated from the perspective of the user they must be
				//given to the constructor flipped
				fast = new FastJavaByteArrayYUVLuminanceSource(fastArray, heightCamera, widthCamera,
																			offsetScanAreaTopPixelsScaled,
																			offsetScanAreaLeftPixelsScaled,
																			heightScanAreaScaled,
																			widthScanAreaScaled).rotateCounterClockwise();
			}
			else
			{
				//while in landscape mode the FastJavaByteArrayYUVLuminanceSource paramater names match up with the names of the calculated values
				fast = new FastJavaByteArrayYUVLuminanceSource(fastArray, widthCamera, heightCamera,
																			offsetScanAreaLeftPixelsScaled,
																			offsetScanAreaTopPixelsScaled,
																			widthScanAreaScaled,
																			heightScanAreaScaled);
			}


			var result = barcodeReader.Decode(fast);
			fastArray.Dispose();
			fastArray = null;

			PerformanceCounter.Stop(start,
					"Decode Time: {0} ms (width: " + widthCamera + ", height: " + heightCamera + ", degrees: " + lastDecodedCameraDisplayRotationDegree + ", rotate: " +
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