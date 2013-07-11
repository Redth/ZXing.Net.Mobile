using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using MonoTouch.AVFoundation;
using MonoTouch.CoreFoundation;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreMedia;
using MonoTouch.CoreVideo;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;
using ZXing.Common;
using ZXing.Mobile;

namespace ZXing.Mobile
{
	public class ZXingScannerView : UIView, IZXingScanner<UIView>
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
		Action<ZXing.Result> resultCallback;
		volatile bool stopped = false;
		//BarcodeReader barcodeReader;

		UIView layerView;
		UIView overlayView = null;

		public string CancelButtonText { get;set; }
		public string FlashButtonText { get;set; }

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		void Setup(RectangleF frame)
		{
			if (overlayView != null)
				overlayView.RemoveFromSuperview ();

			if (UseCustomOverlayView && CustomOverlayView != null)
				overlayView = CustomOverlayView;
			else
				overlayView = new ZXingDefaultOverlayView(this.Frame,
	                              TopText, BottomText, CancelButtonText, FlashButtonText,
	                         	  () => { StopScanning(); resultCallback(null);	}, 
									() => ToggleTorch());

			if (overlayView != null)
			{
				/*UITapGestureRecognizer tapGestureRecognizer = new UITapGestureRecognizer ();

				tapGestureRecognizer.AddTarget (() => {

					var pt = tapGestureRecognizer.LocationInView(overlayView);

					Focus(pt);
		
					Console.WriteLine("OVERLAY TOUCH: " + pt.X + ", " + pt.Y);

				});
				tapGestureRecognizer.CancelsTouchesInView = false;
				tapGestureRecognizer.NumberOfTapsRequired = 1;
				tapGestureRecognizer.NumberOfTouchesRequired = 1;

				overlayView.AddGestureRecognizer (tapGestureRecognizer);*/

				overlayView.Frame = new RectangleF(0, 0, this.Frame.Width, this.Frame.Height);
				overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			}
		}
					
		
		bool torch = false;
		bool analyzing = true;
	



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
			else
				session.AddInput (input);


			previewLayer = new AVCaptureVideoPreviewLayer(session);

			//Framerate set here (15 fps)
			if (previewLayer.RespondsToSelector(new Selector("connection")))
				previewLayer.Connection.VideoMinFrameDuration = new CMTime(1, 10);

			previewLayer.LayerVideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			previewLayer.Frame = this.Frame;
			previewLayer.Position = new PointF(this.Layer.Bounds.Width / 2, (this.Layer.Bounds.Height / 2));

			layerView = new UIView(this.Frame);
			layerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			layerView.Layer.AddSublayer(previewLayer);

			this.AddSubview(layerView);

			ResizePreview(UIApplication.SharedApplication.StatusBarOrientation);

			if (overlayView != null)
			{
				this.AddSubview (overlayView);
				this.BringSubviewToFront (overlayView);

				//overlayView.LayoutSubviews ();
			}

			session.StartRunning ();

			Console.WriteLine ("RUNNING!!!");

			// create a VideoDataOutput and add it to the sesion
			output = new AVCaptureVideoDataOutput () {
				//videoSettings
				VideoSettings = new AVVideoSettings (CVPixelFormatType.CV32BGRA),
			};

			// configure the output
			queue = new MonoTouch.CoreFoundation.DispatchQueue("ZxingScannerView"); // (Guid.NewGuid().ToString());

			var barcodeReader = new BarcodeReader(null, (img) => 	
			{
				var src = new RGBLuminanceSource(img); //, bmp.Width, bmp.Height);

				//Don't try and rotate properly if we're autorotating anyway
				if (ScanningOptions.AutoRotate.HasValue && ScanningOptions.AutoRotate.Value)
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

			}, null, null); //(p, w, h, f) => new RGBLuminanceSource(p, w, h, RGBLuminanceSource.BitmapFormat.Unknown));

			if (ScanningOptions.TryHarder.HasValue)
			{
				Console.WriteLine("TRY_HARDER: " + ScanningOptions.TryHarder.Value);
				barcodeReader.Options.TryHarder = ScanningOptions.TryHarder.Value;
			}
			if (ScanningOptions.PureBarcode.HasValue)
				barcodeReader.Options.PureBarcode = ScanningOptions.PureBarcode.Value;
			if (ScanningOptions.AutoRotate.HasValue)
			{
				Console.WriteLine("AUTO_ROTATE: " + ScanningOptions.AutoRotate.Value);
				barcodeReader.AutoRotate = ScanningOptions.AutoRotate.Value;
			}
			if (!string.IsNullOrEmpty (ScanningOptions.CharacterSet))
				barcodeReader.Options.CharacterSet = ScanningOptions.CharacterSet;
			if (ScanningOptions.TryInverted.HasValue)
				barcodeReader.TryInverted = ScanningOptions.TryInverted.Value;

			if (ScanningOptions.PossibleFormats != null && ScanningOptions.PossibleFormats.Count > 0)
			{
				barcodeReader.Options.PossibleFormats = new List<BarcodeFormat>();
				
				foreach (var pf in ScanningOptions.PossibleFormats)
					barcodeReader.Options.PossibleFormats.Add(pf);
			}

			outputRecorder = new OutputRecorder (ScanningOptions, img => 
			{
				if (!IsAnalyzing)
					return;

				try
				{
					var started = DateTime.Now;
					var rs = barcodeReader.Decode(img);
					var total = DateTime.Now - started;

					Console.WriteLine("Decode Time: " + total.TotalMilliseconds + " ms");

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


			Console.WriteLine("SetupCamera Finished");

			session.AddOutput (output);
			//session.StartRunning ();


			if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ModeContinuousAutoFocus))
			{
				NSError err = null;
				if (captureDevice.LockForConfiguration(out err))
				{
					captureDevice.FocusMode = AVCaptureFocusMode.ModeContinuousAutoFocus;

					if (captureDevice.FocusPointOfInterestSupported)
						captureDevice.FocusPointOfInterest = new PointF(0.5f, 0.5f);

					captureDevice.UnlockForConfiguration();
				}
				else
					Console.WriteLine("Failed to Lock for Config: " + err.Description);
			}

			return true;
		}

		public void ResizePreview (UIInterfaceOrientation orientation)
		{
			previewLayer.Frame = this.Frame;

			if (previewLayer.RespondsToSelector(new Selector("connection")))
			{
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
		}

		public void Focus(PointF pointOfInterest)
		{
			//Get the device
			if (AVMediaType.Video == null)
				return;

			var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);

			if (device == null)
				return;

			//See if it supports focusing on a point
			if (device.FocusPointOfInterestSupported && !device.AdjustingFocus)
			{
				NSError err = null;

				//Lock device to config
				if (device.LockForConfiguration(out err))
				{
					Console.WriteLine("Focusing at point: " + pointOfInterest.X + ", " + pointOfInterest.Y);

					//Focus at the point touched
					device.FocusPointOfInterest = pointOfInterest;
					device.FocusMode = AVCaptureFocusMode.ModeContinuousAutoFocus;
					device.UnlockForConfiguration();
				}
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
	
		#region IZXingScanner implementation
		public void StartScanning (MobileBarcodeScanningOptions options, Action<Result> callback)
		{
			if (!analyzing)
				analyzing = true;

			if (!stopped)
				return;

			Setup (this.Frame);

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

			stopped = false;
		}
		public void StartScanning (Action<Result> callback)
		{
			StartScanning (new MobileBarcodeScanningOptions (), callback);
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

		public void PauseAnalysis ()
		{
			analyzing = false;
		}

		public void ResumeAnalysis ()
		{
			analyzing = true;
		}

		public void SetTorch (bool on)
		{
			try
			{
				NSError err;

				var device = MonoTouch.AVFoundation.AVCaptureDevice.DefaultDeviceWithMediaType(MonoTouch.AVFoundation.AVMediaType.Video);
				device.LockForConfiguration(out err);

				if (on)
				{
					device.TorchMode = MonoTouch.AVFoundation.AVCaptureTorchMode.On;
					device.FlashMode = MonoTouch.AVFoundation.AVCaptureFlashMode.On;
				}
				else
				{
					device.TorchMode = MonoTouch.AVFoundation.AVCaptureTorchMode.Off;
					device.FlashMode = MonoTouch.AVFoundation.AVCaptureFlashMode.Off;
				}

				device.UnlockForConfiguration();
				device = null;

				torch = on;
			}
			catch { }
		}

		public void ToggleTorch()
		{
			SetTorch(!IsTorchOn);
		}

		public void AutoFocus ()
		{
			//Doesn't do much on iOS :(
		}

		public string TopText { get;set; }
		public string BottomText { get;set; }


		public UIView CustomOverlayView { get; set; }
		public bool UseCustomOverlayView { get; set; }
		public MobileBarcodeScanningOptions ScanningOptions { get { return options; } }
		public bool IsAnalyzing { get { return analyzing; } }
		public bool IsTorchOn { get { return torch; } }
		#endregion
	}
}

