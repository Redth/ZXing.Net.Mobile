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
using System.Threading.Tasks;
using System.Threading;

namespace ZXing.Mobile
{
	public class ZXingScannerView : UIView
	{
		public ZXingScannerView(IntPtr handle) : base(handle)
		{
		}

		public ZXingScannerView (RectangleF frame) : base(frame)
		{
		}

		AVCaptureSession session;
		AVCaptureVideoPreviewLayer previewLayer;
		AVCaptureVideoDataOutput output;
		OutputRecorder outputRecorder;
		DispatchQueue queue;
		MobileBarcodeScanningOptions options;
		Action<ZXing.Result> resultCallback;
		volatile bool stopped = false;
		//BarcodeReader barcodeReader;

		UIView layerView;

		public void StartScanning(Action<ZXing.Result> callback, MobileBarcodeScanningOptions options)
		{
			this.options = options;
			this.resultCallback = callback;

			Console.WriteLine("StartScanning");

			this.InvokeOnMainThread(() => {
				if (!SetupCaptureSession())
				{
					//Setup 'simulated' view:
					Console.WriteLine("Capture Session FAILED");
					var simView = new UIView(this.Frame);
					simView.BackgroundColor = UIColor.LightGray;
					this.AddSubview(simView);

				}
			});
		}

		public void StopScanning()
		{
			if (stopped)
				return;

			Console.WriteLine("Stopping...");

			outputRecorder.CancelTokenSource.Cancel();
			session.StopRunning();

			stopped = true;
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

			if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ModeContinuousAutoFocus))
			{
				NSError err = null;
				if (captureDevice.LockForConfiguration(out err))
				{
					captureDevice.FocusMode = AVCaptureFocusMode.ModeContinuousAutoFocus;
					captureDevice.UnlockForConfiguration();
				}
				else
					Console.WriteLine("Failed to Lock for Config: " + err.Description);
			}

			var input = AVCaptureDeviceInput.FromDevice (captureDevice);
			if (input == null){
				Console.WriteLine ("No input - this won't work on the simulator, try a physical device");
				return false;
			}
			else
				session.AddInput (input);
			
			// create a VideoDataOutput and add it to the sesion
			output = new AVCaptureVideoDataOutput () {
				VideoSettings = new AVVideoSettings (CVPixelFormatType.CV32BGRA),
			};

			
			// configure the output
			queue = new MonoTouch.CoreFoundation.DispatchQueue("ZxingScannerView"); // (Guid.NewGuid().ToString());


			var barcodeReader = new BarcodeReader(null, (img) => 	
			{
				using (var bmp = new Bitmap(img))
				{
					var src = new RGBLuminanceSource(bmp, bmp.Width, bmp.Height);

					//Don't try and rotate properly if we're autorotating anyway
					if (options.AutoRotate.HasValue && options.AutoRotate.Value)
						return src;

					switch (UIDevice.CurrentDevice.Orientation)
					{
						case UIDeviceOrientation.Portrait:
							return src.rotateCounterClockwise().rotateCounterClockwise().rotateCounterClockwise();
						case UIDeviceOrientation.PortraitUpsideDown:
							return src.rotateCounterClockwise().rotateCounterClockwise().rotateCounterClockwise();
						case UIDeviceOrientation.LandscapeLeft:
							return src;
						case UIDeviceOrientation.LandscapeRight:
							return src;
					}

					return src;
				}

			}, null, null); //(p, w, h, f) => new RGBLuminanceSource(p, w, h, RGBLuminanceSource.BitmapFormat.Unknown));

			if (this.options.TryHarder.HasValue)
				barcodeReader.TryHarder = this.options.TryHarder.Value;
			if (this.options.PureBarcode.HasValue)
				barcodeReader.PureBarcode = this.options.PureBarcode.Value;
			if (this.options.AutoRotate.HasValue)
				barcodeReader.AutoRotate = this.options.AutoRotate.Value;
			if (!string.IsNullOrEmpty (this.options.CharacterSet))
				barcodeReader.CharacterSet = this.options.CharacterSet;
			if (this.options.TryInverted.HasValue)
				barcodeReader.TryInverted = this.options.TryInverted.Value;

			if (this.options.PossibleFormats != null && this.options.PossibleFormats.Count > 0)
			{
				barcodeReader.PossibleFormats = new List<BarcodeFormat>();
				
				foreach (var pf in this.options.PossibleFormats)
					barcodeReader.PossibleFormats.Add(pf);
			}


			outputRecorder = new OutputRecorder (this.options, img => 
			{
				try
				{
					var rs = barcodeReader.Decode(img);
			
					if (rs != null)
						resultCallback(rs);
				}
				catch (Exception ex)
				{
					Console.WriteLine("DECODE FAILED: " + ex);
				}
			});

			output.AlwaysDiscardsLateVideoFrames = true;
			output.SetSampleBufferDelegate (outputRecorder, queue);

			session.AddOutput (output);
			session.StartRunning ();

			previewLayer = new AVCaptureVideoPreviewLayer(session);

			//Framerate set here (15 fps)
			previewLayer.Connection.VideoMinFrameDuration = new CMTime(1, 10);
			previewLayer.LayerVideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			previewLayer.Frame = this.Frame;
			previewLayer.Position = new PointF(this.Layer.Bounds.Width / 2, (this.Layer.Bounds.Height / 2));

			layerView = new UIView(this.Frame);
			layerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			layerView.Layer.AddSublayer(previewLayer);

			this.AddSubview(layerView);

			ResizePreview(UIApplication.SharedApplication.StatusBarOrientation);

			Console.WriteLine("SetupCamera Finished");

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
			volatile bool working = false;

			[Export ("captureOutput:didDropSampleBuffer:fromConnection:")]
			public void DidDropSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				//Console.WriteLine("DROPPED");
			}

			public CancellationTokenSource CancelTokenSource = new CancellationTokenSource();


			public override void DidOutputSampleBuffer (AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				if ((DateTime.UtcNow - lastAnalysis).TotalMilliseconds < options.DelayBetweenAnalyzingFrames || working
				    || CancelTokenSource.IsCancellationRequested)
					return;

				working = true;
				//Console.WriteLine("SAMPLE");

				lastAnalysis = DateTime.UtcNow;

				try 
				{
					using (var image = ImageFromSampleBuffer (sampleBuffer))
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

				working = false;
			}
			
			UIImage ImageFromSampleBuffer (CMSampleBuffer sampleBuffer)
			{
				UIImage img = null;

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

						img = UIImage.FromImage (cgImage);
					}
				}

				return img;
			}
		}
	
	}
}

