using System;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using MonoTouch.AVFoundation;
using System.Drawing;
using MonoTouch.CoreMedia;
using MonoTouch.CoreVideo;
using MonoTouch.CoreGraphics;
using ZXing.Mobile;
using ZXing.Common;
using System.Collections.Generic;

namespace ZXing.Mobile
{
	public class ZXingScannerView : UIImageView
	{
		public ZXingScannerView(IntPtr handle) : base(handle)
		{
		}

		public ZXingScannerView (RectangleF frame) : base(frame)
		{
		}

		AVCaptureSession session;
		AVCaptureVideoPreviewLayer previewLayer;
		OutputRecorder outputRecorder;
		DispatchQueue queue;
		MobileBarcodeScanningOptions options;
		Action<ZXing.Result> resultCallback;
		volatile bool stopped = false;
		BarcodeReader barcodeReader;

		UIView layerView;

		public void StartScanning(Action<ZXing.Result> callback, MobileBarcodeScanningOptions options)
		{
			this.options = options;
			this.resultCallback = callback;

			//this.options.TryHarder = true;
			this.options.AutoRotate = true;

			if (!SetupCaptureSession())
			{
				//Setup 'simulated' view:

				var simView = new UIView(this.Frame);
				simView.BackgroundColor = UIColor.LightGray;
				this.AddSubview(simView);

			}
		}

		public void StopScanning()
		{
			if (stopped)
				return;

			if (previewLayer != null)
				previewLayer.RemoveFromSuperLayer();

			if (session != null)
				session.StopRunning();
		}

		protected override void Dispose (bool disposing)
		{
			StopScanning();

			base.Dispose (disposing);
		}
		
		bool SetupCaptureSession ()
		{
			// configure the capture session for low resolution, change this if your code
			// can cope with more data or volume
			session = new AVCaptureSession () {
				SessionPreset = AVCaptureSession.Preset352x288
			};
			
			// create a device input and attach it to the session
			var captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType (AVMediaType.Video);
			if (captureDevice == null){
				Console.WriteLine ("No captureDevice - this won't work on the simulator, try a physical device");
				return false;
			}

			NSError err = null;
			if (captureDevice.LockForConfiguration(out err))
			{
				captureDevice.FocusMode = AVCaptureFocusMode.ModeContinuousAutoFocus;
				captureDevice.UnlockForConfiguration();
			}

			var input = AVCaptureDeviceInput.FromDevice (captureDevice);
			if (input == null){
				Console.WriteLine ("No input - this won't work on the simulator, try a physical device");
				return false;
			}
			session.AddInput (input);
			
			// create a VideoDataOutput and add it to the sesion
			var output = new AVCaptureVideoDataOutput () {
				VideoSettings = new AVVideoSettings (CVPixelFormatType.CV32BGRA),
			};
			
			// configure the output
			queue = new MonoTouch.CoreFoundation.DispatchQueue ("myQueue");

			var barcodeReader = new BarcodeReader(null, (img) => 	
			{

				using (var bmp = new Bitmap(img))
					return new RGBLuminanceSource(bmp, bmp.Width, bmp.Height).rotateCounterClockwise();

			}, null, null); //(p, w, h, f) => new RGBLuminanceSource(p, w, h, RGBLuminanceSource.BitmapFormat.Unknown));

			if (this.options.TryHarder.HasValue)
				barcodeReader.TryHarder = this.options.TryHarder.Value;
			if (this.options.PureBarcode.HasValue)
				barcodeReader.PureBarcode = this.options.PureBarcode.Value;
			if (this.options.AutoRotate.HasValue)
				barcodeReader.AutoRotate = this.options.AutoRotate.Value;
			if (!string.IsNullOrEmpty (this.options.CharacterSet))
				barcodeReader.CharacterSet = this.options.CharacterSet;
			
			if (this.options.PossibleFormats != null && this.options.PossibleFormats.Count > 0)
			{
				barcodeReader.PossibleFormats = new List<BarcodeFormat>();
				
				foreach (var pf in this.options.PossibleFormats)
					barcodeReader.PossibleFormats.Add(pf);
			}

			var multiFormatReader = new MultiFormatReader(); // this.options.BuildMultiFormatReader();

			outputRecorder = new OutputRecorder (this.options, img => 
			{
				/*
				var started = DateTime.UtcNow;

				using (var srcbitmap = new Bitmap(img))
				{
					LuminanceSource source = null;
					BinaryBitmap bitmap = null;
					try 
					{
						//Console.WriteLine(screenImage.Width.ToString() + " x " + screenImage.Height.ToString());
						
						var cropY = (int)((img.Size.Height * 0.4) / 2);
						source = new RGBLuminanceSource(srcbitmap, (int)img.Size.Width, (int)img.Size.Height);
							//.crop(0, cropY, (int)img.Size.Width, (int)img.Size.Height - cropY - cropY)
							//.rotateCounterClockwise(); //.crop(0, cropY, 0, screenImage.Height - cropY - cropY);
						
						//Console.WriteLine(source.Width + " x " + source.Height);
						
						bitmap = new BinaryBitmap(new HybridBinarizer(source));
						
						
						try
						{
							var result = multiFormatReader.decodeWithState(bitmap); //

							if(result != null && result.Text!=null)
								resultCallback(result);
						}
						catch (ReaderException)
						{
						}
					}
					catch (Exception ex) 
					{
						Console.WriteLine("Decoding Failed: " + ex);
					}
				}

				var ended = DateTime.UtcNow - started;
				Console.WriteLine("Analyze Time: " + ended.TotalMilliseconds);
				*/

				try
				{
					DateTime started = DateTime.UtcNow;

					var rs = barcodeReader.Decode(img); //, 640, 480, RGBLuminanceSource.BitmapFormat.Unknown);

					var ended = DateTime.UtcNow - started;

					Console.WriteLine("Analyze Time: " + ended.TotalMilliseconds);

					if (rs != null)
						resultCallback(rs);
				}
				catch (Exception ex)
				{
					Console.WriteLine("DECODE FAILED: " + ex);
				}
			});

			output.SetSampleBufferDelegate (outputRecorder, queue);

			session.AddOutput (output);
			session.StartRunning ();

			previewLayer = new AVCaptureVideoPreviewLayer(session);

			//Framerate set here (15 fps)
			previewLayer.Connection.VideoMinFrameDuration = new CMTime(1, 15);
			previewLayer.LayerVideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			//previewLayer.Bounds = this.Layer.Frame;
			previewLayer.Frame = this.Frame;


			previewLayer.Position = new PointF(this.Layer.Bounds.Width / 2, (this.Layer.Bounds.Height / 2));

			layerView = new UIView(this.Frame);
			layerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

			layerView.Layer.AddSublayer(previewLayer);

			this.AddSubview(layerView);

			return true;
		}

		public void ResizePreview (UIInterfaceOrientation orientation)
		{
			previewLayer.Frame = this.Frame;


			switch (orientation)
			{
				case UIInterfaceOrientation.LandscapeLeft:
					previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.LandscapeLeft;
					break;
				case UIInterfaceOrientation.LandscapeRight:
					previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.LandscapeRight;
					break;
				case UIInterfaceOrientation.Portrait:
					previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;
					break;
				case UIInterfaceOrientation.PortraitUpsideDown:
					previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.PortraitUpsideDown;
					break;
			}
		}
	
		public class OutputRecorder : AVCaptureVideoDataOutputSampleBufferDelegate 
		{
			public OutputRecorder(MobileBarcodeScanningOptions options, Action<UIImage> handleImage) : base()
			{
				HandleImage = handleImage;
				this.options = options;
			}

			MobileBarcodeScanningOptions options;
			Action<UIImage> HandleImage;

			DateTime lastAnalysis = DateTime.MinValue;
			volatile bool isWorking = false;

			public override void DidOutputSampleBuffer (AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				if (isWorking)
					return;

				isWorking = true;

				//Only analyze so often
				if ((DateTime.UtcNow - lastAnalysis).TotalMilliseconds < options.DelayBetweenAnalyzingFrames)
					return;
				
				lastAnalysis = DateTime.UtcNow;

				try 
				{
					var image = ImageFromSampleBuffer (sampleBuffer);

					HandleImage(image);
					
					//
					// Although this looks innocent "Oh, he is just optimizing this case away"
					// this is incredibly important to call on this callback, because the AVFoundation
					// has a fixed number of buffers and if it runs out of free buffers, it will stop
					// delivering frames. 
					//	
					sampleBuffer.Dispose ();
				} catch (Exception e){
					Console.WriteLine (e);
				}

				isWorking = false;
			}
			
			UIImage ImageFromSampleBuffer (CMSampleBuffer sampleBuffer)
			{
				// Get the CoreVideo image
				using (var pixelBuffer = sampleBuffer.GetImageBuffer () as CVPixelBuffer)
				{
					// Lock the base address
					pixelBuffer.Lock (0);
					// Get the number of bytes per row for the pixel buffer
					var baseAddress = pixelBuffer.BaseAddress;
					int bytesPerRow = pixelBuffer.BytesPerRow;
					int width = pixelBuffer.Width;
					int height = pixelBuffer.Height;
					var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;
					// Create a CGImage on the RGB colorspace from the configured parameter above
					using (var cs = CGColorSpace.CreateDeviceRGB ())
						using (var context = new CGBitmapContext (baseAddress,width, height, 8, bytesPerRow, cs, (CGImageAlphaInfo) flags))
					using (var cgImage = context.ToImage ())
					{


						pixelBuffer.Unlock (0);

						return UIImage.FromImage (cgImage);
					}
				}
			}
		}
	
	}
}

