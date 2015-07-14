
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Drawing;
using Android.Graphics;
using Android.Content.PM;
using Android.Hardware;
using System.Threading.Tasks;
using System.Threading;

namespace ZXing.Mobile
{
	// based on https://github.com/xamarin/monodroid-samples/blob/master/ApiDemo/Graphics/CameraPreview.cs
    public class ZXingSurfaceView : SurfaceView, ISurfaceHolderCallback, Android.Hardware.Camera.IPreviewCallback, Android.Hardware.Camera.IAutoFocusCallback
	{
		private const int MIN_FRAME_WIDTH = 240;
		private const int MIN_FRAME_HEIGHT = 240;
		private const int MAX_FRAME_WIDTH = 600;
		private const int MAX_FRAME_HEIGHT = 400;
	
		CancellationTokenSource tokenSource;
		ISurfaceHolder surface_holder;
		Android.Hardware.Camera camera;
		MobileBarcodeScanningOptions options;
		Action<ZXing.Result> callback;
		Activity activity;
        bool isAnalyzing = false;
        bool wasScanned = false;
        bool isTorchOn = false;

        static ManualResetEventSlim _cameraLockEvent = new ManualResetEventSlim(true);

		public ZXingSurfaceView (Activity activity)
			: base (activity)
		{			
			this.activity = activity;

            Init ();
		}

	    protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer) 
            : base(javaReference, transfer) 
        {
            Init ();
	    }

        void Init ()
        {
            CheckPermissions ();

            this.surface_holder = Holder;
            this.surface_holder.AddCallback (this);
            this.surface_holder.SetType (SurfaceType.PushBuffers);

            this.tokenSource = new System.Threading.CancellationTokenSource();
        }

		void CheckPermissions()
		{
			var perf = PerformanceCounter.Start ();

            Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "Checking Camera Permissions...");

			if (!PlatformChecks.HasCameraPermission (this.Context))
			{
				var msg = "ZXing.Net.Mobile requires permission to use the Camera (" + Android.Manifest.Permission.Camera + "), but was not found in your AndroidManifest.xml file.";
				Android.Util.Log.Error ("ZXing.Net.Mobile", msg);

				throw new UnauthorizedAccessException (msg);
			}

			PerformanceCounter.Stop (perf, "CheckPermissions took {0}ms");
		}

	    public void SurfaceCreated (ISurfaceHolder holder)
		{
		}
		
		public void SurfaceChanged (ISurfaceHolder holder, Format format, int w, int h)
		{  
            if (camera == null)
                return;

            var perf = PerformanceCounter.Start ();

            var parameters = camera.GetParameters ();
            parameters.PreviewFormat = ImageFormatType.Nv21;


            var availableResolutions = new List<CameraResolution> ();
            foreach (var sps in parameters.SupportedPreviewSizes) {
                availableResolutions.Add (new CameraResolution {
                    Width = sps.Width,
                    Height = sps.Height
                });
            }

            // Try and get a desired resolution from the options selector
            var resolution = options.GetResolution (availableResolutions);

            // If the user did not specify a resolution, let's try and find a suitable one
            if (resolution == null) {
                // Loop through all supported sizes
                foreach (var sps in parameters.SupportedPreviewSizes) {

                    // Find one that's >= 640x360 but <= 1000x1000
                    // This will likely pick the *smallest* size in that range, which should be fine
                    if (sps.Width >= 640 && sps.Width <= 1000 && sps.Height >= 360 && sps.Height <= 1000) {
                        resolution = new CameraResolution {
                            Width = sps.Width,
                            Height = sps.Height
                        };
                        break;
                    }
                }
            }

            // Google Glass requires this fix to display the camera output correctly
            if (Build.Model.Contains ("Glass")) {
                resolution = new CameraResolution {
                    Width = 640,
                    Height = 360
                };
                // Glass requires 30fps
                parameters.SetPreviewFpsRange (30000, 30000);
            }

            // Hopefully a resolution was selected at some point
            if (resolution != null) {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Selected Resolution: " + resolution.Width + "x" + resolution.Height);
                parameters.SetPreviewSize (resolution.Width, resolution.Height);
            }

            camera.SetParameters (parameters);

            SetCameraDisplayOrientation (this.activity);

            camera.SetPreviewDisplay (holder);
            camera.StartPreview ();

            //cameraResolution = new Size(parameters.PreviewSize.Width, parameters.PreviewSize.Height);

            PerformanceCounter.Stop (perf, "SurfaceChanged took {0}ms");

            AutoFocus();
		}
		
		public void SurfaceDestroyed (ISurfaceHolder holder)
		{
			ShutdownCamera ();
		}


		public byte[] rotateCounterClockwise(byte[] data, int width, int height)
		{
			var rotatedData = new byte[data.Length];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++)
					rotatedData[x * height + height - y - 1] = data[x + y * width];
			}
			return rotatedData;
		}
		
        DateTime lastPreviewAnalysis = DateTime.UtcNow;
		BarcodeReader barcodeReader = null;

		Task processingTask;

		public void OnPreviewFrame (byte [] bytes, Android.Hardware.Camera camera)
		{
            if (!isAnalyzing)
                return;
            
			//Check and see if we're still processing a previous frame
			if (processingTask != null && !processingTask.IsCompleted)
				return;
            
			if ((DateTime.UtcNow - lastPreviewAnalysis).TotalMilliseconds < options.DelayBetweenAnalyzingFrames)
				return;

            // Delay a minimum between scans
            if (wasScanned && ((DateTime.UtcNow - lastPreviewAnalysis).TotalMilliseconds < options.DelayBetweenContinuousScans))
                return;

            wasScanned = false;

			var cameraParameters = camera.GetParameters();
			var width = cameraParameters.PreviewSize.Width;
			var height = cameraParameters.PreviewSize.Height;
			//var img = new YuvImage(bytes, ImageFormatType.Nv21, cameraParameters.PreviewSize.Width, cameraParameters.PreviewSize.Height, null);	
			lastPreviewAnalysis = DateTime.UtcNow;

			processingTask = Task.Factory.StartNew (() =>
			{
				try
				{

					if (barcodeReader == null)
					{
						barcodeReader = new BarcodeReader (null, null, null, (p, w, h, f) => 
					                                  new PlanarYUVLuminanceSource (p, w, h, 0, 0, w, h, false));
						//new PlanarYUVLuminanceSource(p, w, h, dataRect.Left, dataRect.Top, dataRect.Width(), dataRect.Height(), false))
					
						if (this.options.TryHarder.HasValue)
							barcodeReader.Options.TryHarder = this.options.TryHarder.Value;
						if (this.options.PureBarcode.HasValue)
							barcodeReader.Options.PureBarcode = this.options.PureBarcode.Value;
						if (!string.IsNullOrEmpty (this.options.CharacterSet))
							barcodeReader.Options.CharacterSet = this.options.CharacterSet;
						if (this.options.TryInverted.HasValue)
							barcodeReader.TryInverted = this.options.TryInverted.Value;
					
						if (this.options.PossibleFormats != null && this.options.PossibleFormats.Count > 0)
						{
							barcodeReader.Options.PossibleFormats = new List<BarcodeFormat> ();
						
							foreach (var pf in this.options.PossibleFormats)
								barcodeReader.Options.PossibleFormats.Add (pf);
						}
					}

					bool rotate = false;
					int newWidth = width;
					int newHeight = height;

					var cDegrees = getCameraDisplayOrientation(this.activity);

					if (cDegrees == 90 || cDegrees == 270)
					{
						rotate = true;
						newWidth = height;
						newHeight = width;
					}
					
					var start = PerformanceCounter.Start();
					
					if (rotate)
						bytes = rotateCounterClockwise(bytes, width, height);
										
					var result = barcodeReader.Decode (bytes, newWidth, newHeight, RGBLuminanceSource.BitmapFormat.Unknown);

						PerformanceCounter.Stop(start, "Decode Time: {0} ms (width: " + width + ", height: " + height + ", degrees: " + cDegrees + ", rotate: " + rotate + ")");
				
					if (result == null || string.IsNullOrEmpty (result.Text))
						return;
				
                    Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "Barcode Found: " + result.Text);
				
                    wasScanned = true;
					callback (result);
				}
				catch (ReaderException)
				{
                    Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "No barcode Found");
					// ignore this exception; it happens every time there is a failed scan
				
				}
				catch (Exception)
				{
					// TODO: this one is unexpected.. log or otherwise handle it
					throw;
				}

			});
		}
		
		
		public void OnAutoFocus (bool success, Android.Hardware.Camera camera)
		{
            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocused");
			
			Task.Factory.StartNew(() => 
			                                             {
				int slept = 0;
				
				while (!tokenSource.IsCancellationRequested && slept < 2000)
				{
					System.Threading.Thread.Sleep(100);
					slept += 100;
				}
				
				if (!tokenSource.IsCancellationRequested)
					AutoFocus();
			});
		}
		
		public override bool OnTouchEvent (MotionEvent e)
		{
			var r = base.OnTouchEvent(e);
			
			AutoFocus();
			
			return r;
		}
		
		public void AutoFocus()
		{
			if (camera != null)
			{
				if (!tokenSource.IsCancellationRequested)
				{
                    Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "AutoFocus Requested");
					camera.AutoFocus(this);
				}
			}
		}
		
		//int cameraDegrees = 0;

		int getCameraDisplayOrientation(Activity context)
		{
			var degrees = 0;

			var display = context.WindowManager.DefaultDisplay;

			var rotation = display.Rotation;

			var displayMetrics = new Android.Util.DisplayMetrics ();

			display.GetMetrics (displayMetrics);

			int width = displayMetrics.WidthPixels;
			int height = displayMetrics.HeightPixels;

			if((rotation == SurfaceOrientation.Rotation0 || rotation == SurfaceOrientation.Rotation180) && height > width ||
				(rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270) && width > height)
			{
				switch(rotation)
				{
				case SurfaceOrientation.Rotation0:
					degrees = 90;
					break;
				case SurfaceOrientation.Rotation90:
					degrees = 0;
					break;
				case SurfaceOrientation.Rotation180:
					degrees = 270;
					break;
				case SurfaceOrientation.Rotation270:
					degrees = 180;
					break;
				}
			}
			//Natural orientation is landscape or square
			else
			{
				switch(rotation)
				{
				case SurfaceOrientation.Rotation0:
					degrees = 0;
					break;
				case SurfaceOrientation.Rotation90:
					degrees = 270;
					break;
				case SurfaceOrientation.Rotation180:
					degrees = 180;
					break;
				case SurfaceOrientation.Rotation270:
					degrees = 90; 
					break;
				}
			}

			return degrees;
		}

		public void SetCameraDisplayOrientation(Activity context) 
		{
			var degrees = getCameraDisplayOrientation (context);

            Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "Changing Camera Orientation to: " + degrees);
			//cameraDegrees = degrees;

			try { camera.SetDisplayOrientation (degrees); }
			catch (Exception ex) {
                Android.Util.Log.Error (MobileBarcodeScanner.TAG, ex.ToString ());
			}
		}
		
		
		public void ShutdownCamera ()
		{
			tokenSource.Cancel();
			
            if (camera == null)
                return;

            var theCamera = camera;
            camera = null;

            // make this asyncronous so that we can return from the view straight away instead of waiting for the camera to release.
            Task.Factory.StartNew(() => {
                try {
                    theCamera.SetPreviewCallback(null);
                    theCamera.StopPreview();
                    theCamera.Release();
                } catch (Exception e) {
                    Android.Util.Log.Error(MobileBarcodeScanner.TAG, e.ToString());
                } finally {
                    ReleaseExclusiveAccess();
                }
            });
		}
		
		
		private void drawResultPoints(Bitmap barcode, ZXing.Result rawResult) 
		{
			var points = rawResult.ResultPoints;
			
			if (points != null && points.Length > 0)
			{
				var canvas = new Canvas(barcode);
				Paint paint = new Paint();
				paint.Color = Android.Graphics.Color.White;
				paint.StrokeWidth = 3.0f;
				paint.SetStyle(Paint.Style.Stroke);
				
				var border = new RectF(2, 2, barcode.Width - 2, barcode.Height - 2);
				canvas.DrawRect(border, paint);
				
				paint.Color = Android.Graphics.Color.Purple;
				
				if (points.Length == 2) 
				{
					paint.StrokeWidth = 4.0f;
					drawLine(canvas, paint, points[0], points[1]);
				} 
				else if (points.Length == 4 &&
				         (rawResult.BarcodeFormat == BarcodeFormat.UPC_A ||
				 rawResult.BarcodeFormat == BarcodeFormat.EAN_13)) 
				{
					// Hacky special case -- draw two lines, for the barcode and metadata
					drawLine(canvas, paint, points[0], points[1]);
					drawLine(canvas, paint, points[2], points[3]);
				}
				else 
				{
					paint.StrokeWidth = 10.0f;
					
					foreach (ResultPoint point in points)
						canvas.DrawPoint(point.X, point.Y, paint);
				}
			}
		}
		
		private void drawLine(Canvas canvas, Paint paint, ResultPoint a, ResultPoint b) 
		{
			canvas.DrawLine(a.X, a.Y, b.X, b.Y, paint);
		}

		public Size FindBestPreviewSize(Android.Hardware.Camera.Parameters p, Size screenRes)
		{
			var max = p.SupportedPreviewSizes.Count;
			
			var s = p.SupportedPreviewSizes[max - 1];
			
			return new Size(s.Width, s.Height);
		}

        private void GetExclusiveAccess()
        {
            Console.WriteLine ("Getting Camera Exclusive access");
            var result = _cameraLockEvent.Wait(TimeSpan.FromSeconds(10));
            if (!result)
                throw new Exception("Couldn't get exclusive access to the camera");

            _cameraLockEvent.Reset();
            Console.WriteLine ("Got Camera Exclusive access");
        }

        private void ReleaseExclusiveAccess()
        {
            // release the camera exclusive access allowing it to be used again.
            Console.WriteLine ("Releasing Exclusive access to camera");
            _cameraLockEvent.Set();
        }

        public void StartScanning (MobileBarcodeScanningOptions options, Action<Result> callback)
        {           
            this.callback = callback;
            this.options = options;

            lastPreviewAnalysis = DateTime.UtcNow.AddMilliseconds(options.InitialDelayBeforeAnalyzingFrames);
            isAnalyzing = true;

            Console.WriteLine ("StartScanning");

            CheckPermissions ();

            var perf = PerformanceCounter.Start ();

            GetExclusiveAccess();

            try 
            {
                var version = Build.VERSION.SdkInt;

                if (version >= BuildVersionCodes.Gingerbread)
                {
                    Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "Checking Number of cameras...");

                    var numCameras = Android.Hardware.Camera.NumberOfCameras;
                    var camInfo = new Android.Hardware.Camera.CameraInfo();
                    var found = false;
                    Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "Found " + numCameras + " cameras...");

                    var whichCamera = CameraFacing.Back;

                    if (options.UseFrontCameraIfAvailable.HasValue && options.UseFrontCameraIfAvailable.Value)
                        whichCamera = CameraFacing.Front;

                    for (int i = 0; i < numCameras; i++)
                    {
                        Android.Hardware.Camera.GetCameraInfo(i, camInfo);
                        if (camInfo.Facing == whichCamera)
                        {
                            Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "Found " + whichCamera + " Camera, opening...");
                            camera = Android.Hardware.Camera.Open(i);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Finding " + whichCamera + " camera failed, opening camera 0...");
                        camera = Android.Hardware.Camera.Open(0);
                    }
                }
                else
                {
                    camera = Android.Hardware.Camera.Open();
                }

                if (camera == null)
                    Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Camera is null :(");

                camera.SetPreviewCallback (this);

            } catch (Exception ex) {
                ShutdownCamera ();

                Console.WriteLine("Setup Error: " + ex);
            }

            PerformanceCounter.Stop (perf, "SurfaceCreated took {0}ms");
        }

        public void StartScanning (Action<Result> callback)
        {
            StartScanning (new MobileBarcodeScanningOptions (), callback);
        }

        public void StopScanning ()
        {
            isAnalyzing = false;
            ShutdownCamera ();
        }

        public void PauseAnalysis ()
        {
            isAnalyzing = false;
        }

        public void ResumeAnalysis ()
        {
            isAnalyzing = true;
        }

        public void SetTorch (bool on)
        {
            if (!this.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFlash))
            {
                Android.Util.Log.Info(MobileBarcodeScanner.TAG, "Flash not supported on this device");
                return;
            }

            if (!PlatformChecks.HasFlashlightPermission (this.Context))
            {
                var msg = "ZXing.Net.Mobile requires permission to use the Flash (" + Android.Manifest.Permission.Flashlight + "), but was not found in your AndroidManifest.xml file.";
                Android.Util.Log.Error (MobileBarcodeScanner.TAG, msg);

                throw new UnauthorizedAccessException (msg);
            }

            if (camera == null)
            {
                Android.Util.Log.Info(MobileBarcodeScanner.TAG, "NULL Camera");
                return;
            }

            var p = camera.GetParameters();
            var supportedFlashModes = p.SupportedFlashModes;

            if (supportedFlashModes == null)
                supportedFlashModes = new List<string>();

            var flashMode=  string.Empty;

            if (on)
            {
                if (supportedFlashModes.Contains(Android.Hardware.Camera.Parameters.FlashModeTorch))
                    flashMode = Android.Hardware.Camera.Parameters.FlashModeTorch;
                else if (supportedFlashModes.Contains(Android.Hardware.Camera.Parameters.FlashModeOn))
                    flashMode = Android.Hardware.Camera.Parameters.FlashModeOn;
                isTorchOn = true;
            }
            else 
            {
                if ( supportedFlashModes.Contains(Android.Hardware.Camera.Parameters.FlashModeOff))
                    flashMode = Android.Hardware.Camera.Parameters.FlashModeOff;
                isTorchOn = false;
            }

            if (!string.IsNullOrEmpty(flashMode))
            {
                p.FlashMode = flashMode;
                camera.SetParameters(p);
            }
        }

        public void ToggleTorch ()
        {
            SetTorch (!isTorchOn);
        }
        public MobileBarcodeScanningOptions ScanningOptions {
            get { return options; }
        }
        public bool IsTorchOn {
            get { return isTorchOn; }
        }
        public bool IsAnalyzing {
            get { return isAnalyzing; }
        }
	}
}

