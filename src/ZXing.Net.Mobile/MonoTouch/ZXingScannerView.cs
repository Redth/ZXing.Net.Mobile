using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using MonoTouch.AVFoundation;
using MonoTouch.CoreFoundation;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreMedia;
using MonoTouch.CoreVideo;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;

namespace ZXing.Mobile
{
	public class ZXingScannerView : UIView
	{
		public ZXingScannerView(IntPtr handle) 
            : base(handle)
		{
		}

		public ZXingScannerView (RectangleF frame) 
            : base(frame)
		{
		}

        public ZXingScannerView()
        {
        }

		AVCaptureSession _session;
		AVCaptureVideoPreviewLayer _previewLayer;
		AVCaptureVideoDataOutput _output;
		OutputRecorder _outputRecorder;
		DispatchQueue _queue;
		MobileBarcodeScanningOptions _options;
		Action<Result> _resultCallback;
		volatile bool _stopped;
		//BarcodeReader barcodeReader;

		UIView _layerView;
        UIView _overlayView;

		public bool UseCustomOverlay { get;set; }
		public UIView CustomOverlay { get;set; }
		public string TopText { get;set; }
		public string BottomText { get;set; }
		public string CancelButtonText { get;set; }
		public string FlashButtonText { get;set; }

		void Setup()
		{
			if (_overlayView != null)
				_overlayView.RemoveFromSuperview ();

		    if (UseCustomOverlay && CustomOverlay != null)
		        _overlayView = CustomOverlay;
		    else
		        _overlayView = new ZXingDefaultOverlayView(Frame,
		                                                   TopText, BottomText, CancelButtonText, FlashButtonText,
		                                                   () =>
		                                                   {
		                                                       StopScanning();
		                                                       _resultCallback(null);
		                                                   },
		                                                   ToggleTorch);

			if (_overlayView != null)
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

                _overlayView.Frame = new RectangleF(0, 0, Frame.Width, Frame.Height);
                _overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			}
		}

		public void StartScanning(Action<Result> callback, MobileBarcodeScanningOptions options)
		{
			Setup();

			_options = options;
			_resultCallback = callback;

			Console.WriteLine("StartScanning");

			InvokeOnMainThread(() => {
				if (!SetupCaptureSession())
				{
					//Setup 'simulated' view:
					Console.WriteLine("Capture Session FAILED");
					var simView = new UIView()
					{
                        Frame = new RectangleF(0, 0, Frame.Width, Frame.Height),
					    BackgroundColor = UIColor.LightGray,
					    AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
					};
				    AddSubview(simView);
				}
			});

			_stopped = false;
		}

		public void StopScanning()
		{
			if (_stopped)
				return;

			Console.WriteLine("Stopping...");

            if (_outputRecorder != null)
			    _outputRecorder.CancelTokenSource.Cancel();

            if (_session.Running)
			    _session.StopRunning();

			_stopped = true;
		}
				
		
		bool _torch;

		public bool IsTorchOn { get { return _torch; } }

		public void ToggleTorch()
		{
			try
			{
				NSError err;

				var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
				device.LockForConfiguration(out err);

				if (!_torch)
				{
					device.TorchMode = AVCaptureTorchMode.On;
					device.FlashMode = AVCaptureFlashMode.On;
				}
				else
				{
					device.TorchMode = AVCaptureTorchMode.Off;
					device.FlashMode = AVCaptureFlashMode.Off;
				}

				device.UnlockForConfiguration();
				device = null;

				_torch = !_torch;
			}
			catch { }

		}

		public void Torch(bool on)
		{
			try
			{
				NSError err;

				var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
				device.LockForConfiguration(out err);

				if (on)
				{
					device.TorchMode = AVCaptureTorchMode.On;
					device.FlashMode = AVCaptureFlashMode.On;
				}
				else
				{
					device.TorchMode = AVCaptureTorchMode.Off;
					device.FlashMode = AVCaptureFlashMode.Off;
				}

				device.UnlockForConfiguration();
				device = null;

				_torch = on;
			}
			catch { }

		}



		bool SetupCaptureSession ()
		{
			// configure the capture session for low resolution, change this if your code
			// can cope with more data or volume
			_session = new AVCaptureSession () {
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
		    
            _session.AddInput (input);


		    _previewLayer = new AVCaptureVideoPreviewLayer(_session);

			//Framerate set here (15 fps)
			if (_previewLayer.RespondsToSelector(new Selector("connection")))
				_previewLayer.Connection.VideoMinFrameDuration = new CMTime(1, 10);

			_previewLayer.LayerVideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			//previewLayer.Frame = this.Frame;
			_previewLayer.Position = new PointF(Layer.Bounds.Width / 2, (Layer.Bounds.Height / 2));

			_layerView = new UIView(Frame);
			//layerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			_layerView.Layer.AddSublayer(_previewLayer);

			AddSubview(_layerView);

			ResizePreview(UIApplication.SharedApplication.StatusBarOrientation);

			if (_overlayView != null)
			{
				AddSubview (_overlayView);
				BringSubviewToFront (_overlayView);

				//overlayView.LayoutSubviews ();
			}

			_session.StartRunning ();

			Console.WriteLine ("RUNNING!!!");

			// create a VideoDataOutput and add it to the sesion
			_output = new AVCaptureVideoDataOutput () {
				//videoSettings
				VideoSettings = new AVVideoSettings (CVPixelFormatType.CV32BGRA),
			};

			// configure the output
			_queue = new DispatchQueue("ZxingScannerView"); // (Guid.NewGuid().ToString());

			var barcodeReader = new BarcodeReader(null, (img) => 	
			{
				var src = new RGBLuminanceSource(img); //, bmp.Width, bmp.Height);

				//Don't try and rotate properly if we're autorotating anyway
				if (_options.AutoRotate.HasValue && _options.AutoRotate.Value)
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

			if (_options.TryHarder.HasValue)
			{
				Console.WriteLine("TRY_HARDER: " + _options.TryHarder.Value);
				barcodeReader.Options.TryHarder = _options.TryHarder.Value;
			}
			if (_options.PureBarcode.HasValue)
				barcodeReader.Options.PureBarcode = _options.PureBarcode.Value;
			if (_options.AutoRotate.HasValue)
			{
				Console.WriteLine("AUTO_ROTATE: " + _options.AutoRotate.Value);
				barcodeReader.AutoRotate = _options.AutoRotate.Value;
			}
			if (!string.IsNullOrEmpty (_options.CharacterSet))
				barcodeReader.Options.CharacterSet = _options.CharacterSet;
			if (_options.TryInverted.HasValue)
				barcodeReader.TryInverted = _options.TryInverted.Value;

			if (_options.PossibleFormats != null && _options.PossibleFormats.Count > 0)
			{
				barcodeReader.Options.PossibleFormats = new List<BarcodeFormat>();
				
				foreach (var pf in _options.PossibleFormats)
					barcodeReader.Options.PossibleFormats.Add(pf);
			}

			_outputRecorder = new OutputRecorder (_options, img => 
			{
				try
				{
					var started = DateTime.Now;
					var rs = barcodeReader.Decode(img);
					var total = DateTime.Now - started;

					Console.WriteLine("Decode Time: " + total.TotalMilliseconds + " ms");

					if (rs != null)
						_resultCallback(rs);
				}
				catch (Exception ex)
				{
					Console.WriteLine("DECODE FAILED: " + ex);
				}
			});

			_output.AlwaysDiscardsLateVideoFrames = true;
			_output.SetSampleBufferDelegate (_outputRecorder, _queue);


			Console.WriteLine("SetupCamera Finished");

			_session.AddOutput (_output);
			//session.StartRunning ();


			if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ModeContinuousAutoFocus))
			{
				NSError err;
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
			_previewLayer.Frame = Frame;

			if (_previewLayer.RespondsToSelector(new Selector("connection")))
			{
				switch (orientation)
				{
					case UIInterfaceOrientation.LandscapeLeft:
						_previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.LandscapeLeft;
						break;
					case UIInterfaceOrientation.LandscapeRight:
						_previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.LandscapeRight;
						break;
					case UIInterfaceOrientation.Portrait:
						_previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;
						break;
					case UIInterfaceOrientation.PortraitUpsideDown:
						_previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.PortraitUpsideDown;
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
				NSError err;

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
			public OutputRecorder(MobileBarcodeScanningOptions options, Action<UIImage> handleImage)
			{
				_handleImage = handleImage;
				_options = options;
			}

		    readonly MobileBarcodeScanningOptions _options;
		    readonly Action<UIImage> _handleImage;

			DateTime _lastAnalysis = DateTime.MinValue;
			volatile bool _working;

			[Export ("captureOutput:didDropSampleBuffer:fromConnection:")]
			public void DidDropSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				//Console.WriteLine("DROPPED");
			}

			public CancellationTokenSource CancelTokenSource = new CancellationTokenSource();


			public override void DidOutputSampleBuffer (AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				if ((DateTime.UtcNow - _lastAnalysis).TotalMilliseconds < _options.DelayBetweenAnalyzingFrames || _working
				    || CancelTokenSource.IsCancellationRequested)
					return;

				_working = true;
				//Console.WriteLine("SAMPLE");

				_lastAnalysis = DateTime.UtcNow;

				try 
				{
					using (var image = ImageFromSampleBuffer (sampleBuffer))
						_handleImage(image);
					
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

				_working = false;
			}

		    static UIImage ImageFromSampleBuffer (CMSampleBuffer sampleBuffer)
			{
				UIImage img = null;

				// Get the CoreVideo image
				using (var pixelBuffer = sampleBuffer.GetImageBuffer () as CVPixelBuffer)
				{
					// Lock the base address
				    if (pixelBuffer != null)
				    {
				        pixelBuffer.Lock (0);
				        // Get the number of bytes per row for the pixel buffer
				        var baseAddress = pixelBuffer.BaseAddress;
				        var bytesPerRow = pixelBuffer.BytesPerRow;
				        var width = pixelBuffer.Width;
				        var height = pixelBuffer.Height;
				    
				        const CGBitmapFlags flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;
					    // Create a CGImage on the RGB colorspace from the configured parameter above
					    using (var cs = CGColorSpace.CreateDeviceRGB ())
					    using (var context = new CGBitmapContext (baseAddress,width, height, 8, bytesPerRow, cs, (CGImageAlphaInfo) flags))
					    using (var cgImage = context.ToImage ())
					    {
						    pixelBuffer.Unlock (0);

						    img = UIImage.FromImage (cgImage);
					    }
                    }
				}

				return img;
			}
		}
	
	}
}

