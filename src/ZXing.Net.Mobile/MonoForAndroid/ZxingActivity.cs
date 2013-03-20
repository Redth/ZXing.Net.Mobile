using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Hardware;
using Android.Graphics;

using Android.Content;
using Android.Runtime;
using Android.Widget;

using ZXing;

namespace ZXing.Mobile
{
	[Activity (Label = "Scanner", ScreenOrientation=ScreenOrientation.Portrait)] //, ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.KeyboardHidden)] // ScreenOrientation = ScreenOrientation.Portrait)]
	public class ZxingActivity : Activity 
	{
		public static event Action<ZXing.Result> OnScanCompleted;
		public static event Action OnCanceled;

		public static event Action OnCancelRequested;
		public static event Action<bool> OnTorchRequested;
		public static event Action OnAutoFocusRequested;

		public static void RequestCancel ()
		{
			var evt = OnCancelRequested;
			if (evt != null)
				evt();
		}

		public static void RequestTorch (bool torchOn)
		{
			var evt = OnTorchRequested;
			if (evt != null)
				evt(torchOn);
		}

		public static void RequestAutoFocus ()
		{
			var evt = OnAutoFocusRequested;
			if (evt != null)
				evt();
		}

		public static View CustomOverlayView { get;set; }
		public static bool UseCustomView { get; set; }
		public static MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public static string TopText { get;set; }
		public static string BottomText { get;set; }

		ZxingSurfaceView scanner;
		ZxingOverlayView zxingOverlay;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			this.RequestWindowFeature (WindowFeatures.NoTitle);

			this.Window.AddFlags (WindowManagerFlags.Fullscreen); //to show
			this.Window.AddFlags (WindowManagerFlags.KeepScreenOn); //Don't go to sleep while scanning

			scanner = new ZxingSurfaceView (this, ScanningOptions);
			SetContentView (scanner);

			var layoutParams = new LinearLayout.LayoutParams (ViewGroup.LayoutParams.FillParent, ViewGroup.LayoutParams.FillParent);

			if (!UseCustomView)
			{
				zxingOverlay = new ZxingOverlayView (this);
				zxingOverlay.TopText = TopText ?? "";
				zxingOverlay.BottomText = BottomText ?? "";

				this.AddContentView (zxingOverlay, layoutParams);
			}
			else if (CustomOverlayView != null)
			{
				this.AddContentView(CustomOverlayView, layoutParams);
			}

			OnCancelRequested += () => {
				this.CancelScan();
			};

			OnAutoFocusRequested += () => {
				this.AutoFocus();
			};

			OnTorchRequested += (bool on) => {
				this.SetTorch(on);
			};

		}

		public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig)
		{
			base.OnConfigurationChanged (newConfig);

			Android.Util.Log.Debug("ZXING", "Configuration Changed");
		}

		public void SetTorch(bool on)
		{
			this.scanner.Torch(on);
		}

		public void AutoFocus()
		{
			this.scanner.AutoFocus();
		}
		public void CancelScan ()
		{
			scanner.ShutdownCamera();
			Finish ();
			var evt = OnCanceled;
			if (evt !=null)
				evt();
		}
		public override bool OnKeyDown (Keycode keyCode, KeyEvent e)
		{
			switch (keyCode)
			{
			case Keycode.Back:
				CancelScan();
				break;
			case Keycode.Focus:
				return true;
			}

			return base.OnKeyDown (keyCode, e);
		}

		public virtual void OnScan (ZXing.Result result)
		{
			var evt = OnScanCompleted;
			if (evt != null)
				evt(result);

			this.Finish();
		}
	}
	// based on https://github.com/xamarin/monodroid-samples/blob/master/ApiDemo/Graphics/CameraPreview.cs
	public class ZxingSurfaceView : SurfaceView, ISurfaceHolderCallback, Android.Hardware.Camera.IPreviewCallback, Android.Hardware.Camera.IAutoFocusCallback
	{
		private const int MIN_FRAME_WIDTH = 240;
		private const int MIN_FRAME_HEIGHT = 240;
		private const int MAX_FRAME_WIDTH = 600;
		private const int MAX_FRAME_HEIGHT = 400;

		Size screenResolution = Size.Empty;
		Size cameraResolution = Size.Empty;

		System.Threading.CancellationTokenSource tokenSource;
		DateTime lastAutoFocus = DateTime.MinValue;
		ZxingActivity activity;
		ISurfaceHolder surface_holder;
		Android.Hardware.Camera camera;
		MobileBarcodeScanningOptions options;

		int width, height;
		MultiFormatReader reader;
		//BarcodeReader reader;

		public ZxingSurfaceView (ZxingActivity activity, MobileBarcodeScanningOptions options)
			: base (activity)
		{
			this.activity = activity;

			screenResolution = new Size(this.activity.WindowManager.DefaultDisplay.Width, this.activity.WindowManager.DefaultDisplay.Height);

			this.options = options;
			lastPreviewAnalysis = DateTime.Now.AddMilliseconds(options.InitialDelayBeforeAnalyzingFrames);

			this.reader = this.options.BuildMultiFormatReader();

			this.surface_holder = Holder;
			this.surface_holder.AddCallback (this);
			this.surface_holder.SetType (SurfaceType.PushBuffers);
			
			this.tokenSource = new System.Threading.CancellationTokenSource();
		}

		public void SurfaceCreated (ISurfaceHolder holder)
		{
			try 
			{
#if __ANDROID_9__
				var numCameras = Android.Hardware.Camera.NumberOfCameras;
				var camInfo = new Android.Hardware.Camera.CameraInfo();
				var found = false;

				for (int i = 0; i < numCameras; i++)
				{
					Android.Hardware.Camera.GetCameraInfo(i, camInfo);
					if (camInfo.Facing == CameraFacing.Back)
					{
						camera = Android.Hardware.Camera.Open(i);
						found = true;
						break;
					}
				}

				if (!found)
				{
					Android.Util.Log.Debug("ZXing.Net.Mobile", "Finding rear camera failed, opening camera 0...");
					camera = Android.Hardware.Camera.Open(0);
				}
#else
				camera = Android.Hardware.Camera.Open();
#endif
				if (camera == null)
					Android.Util.Log.Debug("ZXing.Net.Mobile", "Camera is null :(");
				
				//camera = Android.Hardware.Camera.Open ();
				camera.SetPreviewDisplay (holder);
				camera.SetPreviewCallback (this);

			} catch (Exception) {
				ShutdownCamera ();

				// TODO: log or otherwise handle this exception

				//throw;
			}
		}

		public void SurfaceChanged (ISurfaceHolder holder, global::Android.Graphics.Format format, int w, int h)
		{
			if (camera == null)
				return;

			Android.Hardware.Camera.Parameters parameters = camera.GetParameters ();

			width = parameters.PreviewSize.Width;
			height = parameters.PreviewSize.Height;
			//parameters.PreviewFormat = ImageFormatType.Rgb565;
			//parameters.PreviewFrameRate = 15;
			
			parameters.PreviewFormat = ImageFormatType.Nv21;

			camera.SetParameters (parameters);
			camera.SetDisplayOrientation (90);
			camera.StartPreview ();

			cameraResolution = new Size(parameters.PreviewSize.Width, parameters.PreviewSize.Height);

			AutoFocus();
		}

		public void SurfaceDestroyed (ISurfaceHolder holder)
		{
			ShutdownCamera ();
		}

		DateTime lastPreviewAnalysis = DateTime.Now;

		public void OnPreviewFrame (byte [] bytes, Android.Hardware.Camera camera)
		{
			if ((DateTime.Now - lastPreviewAnalysis).TotalMilliseconds < options.DelayBetweenAnalyzingFrames)
				return;
			
			try 
			{
				/* OLD Android Code
				//Fix for image not rotating on devices
				byte[] rotatedData = new byte[bytes.Length];
				for (int y = 0; y < height; y++) {
				    for (int x = 0; x < width; x++)
				        rotatedData[x * height + height - y - 1] = bytes[x + y * width];
				}
				
				var cameraParameters = camera.GetParameters();

				//Changed to using a YUV Image to get the byte data instead of manually working with it!
				var img = new YuvImage(rotatedData, ImageFormatType.Nv21, cameraParameters.PreviewSize.Width, cameraParameters.PreviewSize.Height, null);	
				var dataRect = GetFramingRectInPreview();
			
				var luminance = new PlanarYUVLuminanceSource (img.GetYuvData(), width, height, dataRect.Left, dataRect.Top, dataRect.Width(), dataRect.Height(), false);
				//var luminance = new PlanarYUVLuminanceSource(img.GetYuvData(), cameraParameters.PreviewSize.Width, cameraParameters.PreviewSize.Height, 0, 0, cameraParameters.PreviewSize.Width, cameraParameters.PreviewSize.Height, false);
				var binarized = new BinaryBitmap (new ZXing.Common.HybridBinarizer(luminance));
				var result = reader.decodeWithState(binarized);
				*/
				
				
				
				var cameraParameters = camera.GetParameters();
				var img = new YuvImage(bytes, ImageFormatType.Nv21, cameraParameters.PreviewSize.Width, cameraParameters.PreviewSize.Height, null);	
				var dataRect = GetFramingRectInPreview();
				
				//var barcodeReader = new BarcodeReader(null, p => new PlanarYUVLuminanceSource(img.GetYuvData(), img.Width, img.Height, dataRect.Left, dataRect.Top,
				//                                            dataRect.Width(), dataRect.Height(), false), null, null)
				//{
				//	AutoRotate = true,
				//	TryHarder = true,
				//};

				var barcodeReader = new BarcodeReader(null, null, null, (p, w, h, f) => 
				    new PlanarYUVLuminanceSource(p, w, h, 0, 0, w, h, false))
					//new PlanarYUVLuminanceSource(p, w, h, dataRect.Left, dataRect.Top, dataRect.Width(), dataRect.Height(), false))
				{
					AutoRotate = true,
					TryHarder = false
				};

				if (this.options.PureBarcode.HasValue && this.options.PureBarcode.Value)
					barcodeReader.PureBarcode = this.options.PureBarcode.Value;

				if (this.options.PossibleFormats != null && this.options.PossibleFormats.Count > 0)
					barcodeReader.PossibleFormats = this.options.PossibleFormats;

				var result = barcodeReader.Decode(img.GetYuvData(), img.Width, img.Height, RGBLuminanceSource.BitmapFormat.Unknown);


				lastPreviewAnalysis = DateTime.Now;

				if (result == null || string.IsNullOrEmpty (result.Text))
					return;

				Android.Util.Log.Debug("ZXing.Mobile", "Barcode Found: " + result.Text);

				ShutdownCamera();

				activity.OnScan (result);

			} catch (ReaderException) {
				Android.Util.Log.Debug("ZXing.Mobile", "No barcode Found");
				// ignore this exception; it happens every time there is a failed scan

			} catch (Exception){

				// TODO: this one is unexpected.. log or otherwise handle it

				throw;
			}
		}


		public void OnAutoFocus (bool success, Android.Hardware.Camera camera)
		{
			Android.Util.Log.Debug("ZXing.Mobile", "AutoFocused");

			System.Threading.Tasks.Task.Factory.StartNew(() => 
			{
				int slept = 0;

				while (!tokenSource.IsCancellationRequested && slept < 2000)
				{
					System.Threading.Thread.Sleep(100);
					slept += 100;
				}

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
					Android.Util.Log.Debug("ZXING", "AutoFocus Requested");
					camera.AutoFocus(this);
				}
			}
		}

		public void Torch(bool on)
		{
			if (!this.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFlash))
			{
				Android.Util.Log.Info("ZXING", "Flash not supported on this device");
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
			}
			else 
			{
				if ( supportedFlashModes.Contains(Android.Hardware.Camera.Parameters.FlashModeOff))
					flashMode = Android.Hardware.Camera.Parameters.FlashModeOff;
			}

			if (!string.IsNullOrEmpty(flashMode))
			{
				p.FlashMode = flashMode;
				camera.SetParameters(p);
			}
		}


		public void ShutdownCamera ()
		{
			tokenSource.Cancel();

			if (camera != null) {
				camera.SetPreviewCallback (null);
				camera.StopPreview ();
				camera.Release ();
				camera = null;
			}
		}


		private void drawResultPoints(Android.Graphics.Bitmap barcode, ZXing.Result rawResult) 
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

		Rect framingRect = null;
		Rect framingRectInPreview = null;

		public Rect GetFramingRect() 
		{
	    	if (framingRect == null) 
			{
	      		if (camera == null) 
					return null;
			}

			if (screenResolution == Size.Empty)
				return null;

	      	int width = screenResolution.Width * 15 / 16;
	      	if (width < MIN_FRAME_WIDTH)
				width = MIN_FRAME_WIDTH;
		 	else if (width > MAX_FRAME_WIDTH)
		    	width = MAX_FRAME_WIDTH;

			int height = screenResolution.Height * 4/ 10;
			if (height < MIN_FRAME_HEIGHT)
				height = MIN_FRAME_HEIGHT;
			else if (height > MAX_FRAME_HEIGHT)
				height = MAX_FRAME_HEIGHT;
	      
			int leftOffset = (screenResolution.Width - width) / 2;
			int topOffset = (screenResolution.Height - height) / 2;
			framingRect = new Rect(leftOffset, topOffset, leftOffset + width, topOffset + height);

			Android.Util.Log.Debug("ZXING", "Framing Rect: X=" + framingRect.Left + " Y=" + framingRect.Top + " W=" + framingRect.Width() + " H=" + framingRect.Height());
			return framingRect;
		}

		public Rect GetFramingRectInPreview() 
		{
			if (framingRectInPreview == null)
			{
				var fr = GetFramingRect();
				if (fr == null)
					return null;
			
				var rect = new Rect(fr.Left, fr.Top, fr.Right, fr.Bottom);

				if (cameraResolution == Size.Empty || screenResolution == Size.Empty)
					return null;

				framingRectInPreview = new Rect(rect.Left * cameraResolution.Width / screenResolution.Width,
				                   		rect.Top * cameraResolution.Height / screenResolution.Height,
				                   		rect.Right * cameraResolution.Width / screenResolution.Width,
				                   		rect.Bottom * cameraResolution.Height / screenResolution.Height);

				//rect.Left = rect.Left * cameraResolution.Width / screenResolution.Width;
				//rect.Right = rect.Right * cameraResolution.Width / screenResolution.Width;
				//rect.Top = rect.Top * cameraResolution.Height / screenResolution.Height;
				//rect.Bottom = rect.Bottom * cameraResolution.Height / screenResolution.Height;
				//framingRectInPreview = new Rectangle(rect.Left * cameraResolution.Width / screenResolution.Width,

			}

			Android.Util.Log.Debug("ZXING", "Preview Framing Rect: X=" + framingRectInPreview.Left + " Y=" + framingRectInPreview.Top + " W=" + framingRectInPreview.Width() + " H=" + framingRectInPreview.Height());

			return framingRectInPreview;
		}

		public Size FindBestPreviewSize(Android.Hardware.Camera.Parameters p, Size screenRes)
		{
			var max = p.SupportedPreviewSizes.Count;

			var s = p.SupportedPreviewSizes[max - 1];

			return new Size(s.Width, s.Height);
		}



	}
}