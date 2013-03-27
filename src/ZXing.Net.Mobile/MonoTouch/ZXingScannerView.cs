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
		MultiFormatReader multiFormatReader = null;
		DateTime lastAnalysis = DateTime.MinValue;
		UIView layerView;

		public void StartScanning(Action<ZXing.Result> callback, MobileBarcodeScanningOptions options)
		{
			this.options = options;
			this.resultCallback = callback;

			//this.options.TryHarder = true;
			//this.options.AutoRotate = true;

			multiFormatReader = this.options.BuildMultiFormatReader();

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
				SessionPreset = AVCaptureSession.Preset640x480
			};
			
			// create a device input and attach it to the session
			var captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType (AVMediaType.Video);
			if (captureDevice == null){
				Console.WriteLine ("No captureDevice - this won't work on the simulator, try a physical device");
				return false;
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
			outputRecorder = new OutputRecorder (img => 
			{
				//Only analyze so often
				if ((DateTime.UtcNow - lastAnalysis).TotalMilliseconds < options.DelayBetweenAnalyzingFrames)
					return;

				lastAnalysis = DateTime.UtcNow;


				using (var srcbitmap = new Bitmap(img))
				{
					LuminanceSource source = null;
					BinaryBitmap bitmap = null;
					try 
					{
						source = new RGBLuminanceSource(srcbitmap, srcbitmap.Width, srcbitmap.Height); //.crop(0, cropY, 0, screenImage.Height - cropY - cropY);

						bitmap = new BinaryBitmap(new HybridBinarizer(source.rotateCounterClockwise()));
					
						
						try
						{
						
							var result = multiFormatReader.decodeWithState(bitmap); //

							if(result != null && result.Text!=null)
							{
								Console.WriteLine("W=" + bitmap.Width + ", H=" + bitmap.Height);
								Console.WriteLine("RESULT: " + result.Text);
								//BeepOrVibrate();
								resultCallback(result);
							}
						}
						catch (ReaderException)
						{
						}
					} 
					catch (Exception ex) 
					{
						Console.WriteLine(ex.Message);
					}
					finally 
					{
						if(bitmap!=null)
							bitmap = null;
						
						if(source!=null)
							source = null;
					}	
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

			layerView.Layer.AddSublayer(previewLayer);

			this.AddSubview(layerView);

			return true;
		}
	
		public class OutputRecorder : AVCaptureVideoDataOutputSampleBufferDelegate 
		{
			public OutputRecorder(Action<UIImage> handleImage) : base()
			{
				HandleImage = handleImage;
			}

			Action<UIImage> HandleImage;

			public override void DidOutputSampleBuffer (AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
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

