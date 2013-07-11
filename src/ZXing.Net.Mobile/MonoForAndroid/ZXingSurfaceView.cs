
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

namespace ZXing.Mobile
{
	// based on https://github.com/xamarin/monodroid-samples/blob/master/ApiDemo/Graphics/CameraPreview.cs
	public class ZXingSurfaceView : SurfaceView, ISurfaceHolderCallback, Android.Hardware.Camera.IPreviewCallback, Android.Hardware.Camera.IAutoFocusCallback
	{
		private const int MIN_FRAME_WIDTH = 240;
		private const int MIN_FRAME_HEIGHT = 240;
		private const int MAX_FRAME_WIDTH = 600;
		private const int MAX_FRAME_HEIGHT = 400;
	
		System.Threading.CancellationTokenSource tokenSource;
		ISurfaceHolder surface_holder;
		Android.Hardware.Camera camera;
		MobileBarcodeScanningOptions options;
		Action<ZXing.Result> callback;

		public ZXingSurfaceView (Activity activity, MobileBarcodeScanningOptions options, Action<ZXing.Result> callback)
			: base (activity)
		{
			//this.activity = activity;
			this.callback = callback;
			this.options = options;

			lastPreviewAnalysis = DateTime.Now.AddMilliseconds(options.InitialDelayBeforeAnalyzingFrames);

			this.surface_holder = Holder;
			this.surface_holder.AddCallback (this);
			this.surface_holder.SetType (SurfaceType.PushBuffers);
			
			this.tokenSource = new System.Threading.CancellationTokenSource();
		}

	    protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer) 
            : base(javaReference, transfer) 
        {
            lastPreviewAnalysis = DateTime.Now.AddMilliseconds(options.InitialDelayBeforeAnalyzingFrames);

            this.surface_holder = Holder;
            this.surface_holder.AddCallback(this);
            this.surface_holder.SetType(SurfaceType.PushBuffers);

            this.tokenSource = new System.Threading.CancellationTokenSource();
	    }

	    public void SurfaceCreated (ISurfaceHolder holder)
		{
			try 
			{
				var version = Android.OS.Build.VERSION.SdkInt;

				if (version >= BuildVersionCodes.Gingerbread)
				{
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
				}
				else
				{
					camera = Android.Hardware.Camera.Open();
				}
				if (camera == null)
					Android.Util.Log.Debug("ZXing.Net.Mobile", "Camera is null :(");
				
				
				//camera = Android.Hardware.Camera.Open ();
				camera.SetPreviewDisplay (holder);
				camera.SetPreviewCallback (this);
				
			} catch (Exception ex) {
				ShutdownCamera ();
				
				// TODO: log or otherwise handle this exception
				Console.WriteLine("Setup Error: " + ex);
				//throw;
			}
		}
		
		public void SurfaceChanged (ISurfaceHolder holder, global::Android.Graphics.Format format, int w, int h)
		{
			if (camera == null)
				return;
			
			var parameters = camera.GetParameters ();
			parameters.PreviewFormat = ImageFormatType.Nv21;
			
			camera.SetParameters (parameters);
			camera.SetDisplayOrientation (90);
			camera.StartPreview ();
			
			//cameraResolution = new Size(parameters.PreviewSize.Width, parameters.PreviewSize.Height);
			
			AutoFocus();
		}
		
		public void SurfaceDestroyed (ISurfaceHolder holder)
		{
			ShutdownCamera ();
		}
		
		DateTime lastPreviewAnalysis = DateTime.Now;
		BarcodeReader barcodeReader = null;
		
		public void OnPreviewFrame (byte [] bytes, Android.Hardware.Camera camera)
		{
			if ((DateTime.Now - lastPreviewAnalysis).TotalMilliseconds < options.DelayBetweenAnalyzingFrames)
				return;
			
			try 
			{
				var cameraParameters = camera.GetParameters();
				var img = new YuvImage(bytes, ImageFormatType.Nv21, cameraParameters.PreviewSize.Width, cameraParameters.PreviewSize.Height, null);	
				
				if (barcodeReader == null)
				{
					barcodeReader = new BarcodeReader(null, null, null, (p, w, h, f) => 
					                                  new PlanarYUVLuminanceSource(p, w, h, 0, 0, w, h, false));
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
						barcodeReader.Options.PossibleFormats = new List<BarcodeFormat>();
						
						foreach (var pf in this.options.PossibleFormats)
							barcodeReader.Options.PossibleFormats.Add(pf);
					}
					
					//Always autorotate on android
					barcodeReader.AutoRotate = true;
				}
				
				//Try and decode the result
				var result = barcodeReader.Decode(img.GetYuvData(), img.Width, img.Height, RGBLuminanceSource.BitmapFormat.Unknown);
				
				lastPreviewAnalysis = DateTime.Now;
				
				if (result == null || string.IsNullOrEmpty (result.Text))
					return;
				
				Android.Util.Log.Debug("ZXing.Mobile", "Barcode Found: " + result.Text);
				
				ShutdownCamera();

				callback(result);

			} catch (ReaderException) {
				Android.Util.Log.Debug("ZXing.Mobile", "No barcode Found");
				// ignore this exception; it happens every time there is a failed scan
				
			} catch (Exception) {
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
			
			if (camera == null)
			{
				Android.Util.Log.Info("ZXING", "NULL Camera");
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

		public Size FindBestPreviewSize(Android.Hardware.Camera.Parameters p, Size screenRes)
		{
			var max = p.SupportedPreviewSizes.Count;
			
			var s = p.SupportedPreviewSizes[max - 1];
			
			return new Size(s.Width, s.Height);
		}
	}
}

