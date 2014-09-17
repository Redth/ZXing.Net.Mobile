using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

#if __UNIFIED__
using Foundation;
using CoreFoundation;
using AVFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using ObjCRuntime;
using UIKit;
#else
using MonoTouch.AVFoundation;
using MonoTouch.CoreFoundation;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreMedia;
using MonoTouch.CoreVideo;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;

using CGRect = global::System.Drawing.RectangleF;
using CGPoint = global::System.Drawing.PointF;
#endif

using ZXing.Common;
using ZXing.Mobile;

namespace ZXing.Mobile
{
	public class ZXingScannerView : UIView, IZXingScanner<UIView>
	{
		public delegate void ScannerSetupCompleteDelegate();
		public event ScannerSetupCompleteDelegate OnScannerSetupComplete;

		public ZXingScannerView()
		{
		}

		public ZXingScannerView(IntPtr handle) : base(handle)
		{
		}

		public ZXingScannerView (CGRect frame) : base(frame)
		{
		}

		AVCaptureSession session;
		AVCaptureVideoPreviewLayer previewLayer;
		AVCaptureVideoDataOutput output;
		OutputRecorder outputRecorder;
		DispatchQueue queue;
		Action<ZXing.Result> resultCallback;
		volatile bool stopped = true;
		//BarcodeReader barcodeReader;

		UIView layerView;
		UIView overlayView = null;


		public string CancelButtonText { get;set; }
		public string FlashButtonText { get;set; }

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		void Setup(CGRect frame)
		{
			var started = DateTime.UtcNow;

			if (overlayView != null)
				overlayView.RemoveFromSuperview ();

			if (UseCustomOverlayView && CustomOverlayView != null)
				overlayView = CustomOverlayView;
			else
				overlayView = new ZXingDefaultOverlayView (new CGRect(0, 0, this.Frame.Width, this.Frame.Height),
				                                          TopText, BottomText, CancelButtonText, FlashButtonText,
				                                          () => { StopScanning (); resultCallback (null); }, ToggleTorch);

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

				overlayView.Frame = new CGRect(0, 0, this.Frame.Width, this.Frame.Height);
				overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			}

			var total = DateTime.UtcNow - started;
			Console.WriteLine ("ZXingScannerView.Setup() took {0} ms.", total.TotalMilliseconds);
		}
					
		
		bool torch = false;
		bool analyzing = true;
	

		bool SetupCaptureSession ()
		{
			var started = DateTime.UtcNow;

			var availableResolutions = new List<CameraResolution> ();

			var consideredResolutions = new Dictionary<NSString, CameraResolution> {
				{ AVCaptureSession.Preset352x288, new CameraResolution 	 { Width = 352,  Height = 288 } },
				{ AVCaptureSession.PresetMedium, new CameraResolution 	 { Width = 480,  Height = 360 } },	//480x360
				{ AVCaptureSession.Preset640x480, new CameraResolution 	 { Width = 640,  Height = 480 } },
				{ AVCaptureSession.Preset1280x720, new CameraResolution  { Width = 1280, Height = 720 } },
				{ AVCaptureSession.Preset1920x1080, new CameraResolution { Width = 1920, Height = 1080 } }
			};
				
			// configure the capture session for low resolution, change this if your code
			// can cope with more data or volume
			session = new AVCaptureSession () {
				SessionPreset = AVCaptureSession.Preset640x480
			};
			
			// create a device input and attach it to the session
//			var captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType (AVMediaType.Video);
			AVCaptureDevice captureDevice = null;
			var devices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);
			foreach (var device in devices)
			{
				captureDevice = device;
				if (options.UseFrontCameraIfAvailable.HasValue &&
				    options.UseFrontCameraIfAvailable.Value &&
				    device.Position == AVCaptureDevicePosition.Front)

					break; //Front camera successfully set
				else if (device.Position == AVCaptureDevicePosition.Back && (!options.UseFrontCameraIfAvailable.HasValue || !options.UseFrontCameraIfAvailable.Value))
					break; //Back camera succesfully set
			}
			if (captureDevice == null){
				Console.WriteLine ("No captureDevice - this won't work on the simulator, try a physical device");
				if (overlayView != null)
				{
					this.AddSubview (overlayView);
					this.BringSubviewToFront (overlayView);
				}
				return false;
			}

			CameraResolution resolution = null;

			// Find resolution
			// Go through the resolutions we can even consider
			foreach (var cr in consideredResolutions) {
				// Now check to make sure our selected device supports the resolution
				// so we can add it to the list to pick from
				if (captureDevice.SupportsAVCaptureSessionPreset (cr.Key))
					availableResolutions.Add (cr.Value);
			}

			resolution = options.GetResolution (availableResolutions);

			// See if the user selected a resolution
			if (resolution != null) {
				// Now get the preset string from the resolution chosen
				var preset = (from c in consideredResolutions
							  where c.Value.Width == resolution.Width
								&& c.Value.Height == resolution.Height
							  select c.Key).FirstOrDefault ();

				// If we found a matching preset, let's set it on the session
				if (!string.IsNullOrEmpty(preset))
					session.SessionPreset = preset;
			}

			var input = AVCaptureDeviceInput.FromDevice (captureDevice);
			if (input == null){
				Console.WriteLine ("No input - this won't work on the simulator, try a physical device");
				if (overlayView != null)
				{
					this.AddSubview (overlayView);
					this.BringSubviewToFront (overlayView);
				}				
				return false;
			}
			else
				session.AddInput (input);


			var startedAVPreviewLayerAlloc = DateTime.UtcNow;

			previewLayer = new AVCaptureVideoPreviewLayer(session);

			var totalAVPreviewLayerAlloc = DateTime.UtcNow - startedAVPreviewLayerAlloc;

			Console.WriteLine ("PERF: Alloc AVCaptureVideoPreviewLayer took {0} ms.", totalAVPreviewLayerAlloc.TotalMilliseconds);


			//Framerate set here (15 fps)
			if (UIDevice.CurrentDevice.CheckSystemVersion (7, 0))
			{
				var perf1 = PerformanceCounter.Start ();

				NSError lockForConfigErr = null;

				captureDevice.LockForConfiguration (out lockForConfigErr);
				if (lockForConfigErr == null)
				{
					captureDevice.ActiveVideoMinFrameDuration = new CMTime (1, 10);
					captureDevice.UnlockForConfiguration ();
				}

				PerformanceCounter.Stop (perf1, "PERF: ActiveVideoMinFrameDuration Took {0} ms");
			}
            else
                previewLayer.Connection.VideoMinFrameDuration = new CMTime(1, 10);


			var perf2 = PerformanceCounter.Start ();

			#if __UNIFIED__
			previewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			#else
			previewLayer.LayerVideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			#endif
			previewLayer.Frame = new CGRect(0, 0, this.Frame.Width, this.Frame.Height);
			previewLayer.Position = new CGPoint(this.Layer.Bounds.Width / 2, (this.Layer.Bounds.Height / 2));

			layerView = new UIView(new CGRect(0, 0, this.Frame.Width, this.Frame.Height));
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

			PerformanceCounter.Stop (perf2, "PERF: Setting up layers took {0} ms");

			var perf3 = PerformanceCounter.Start ();

			session.StartRunning ();

			PerformanceCounter.Stop (perf3, "PERF: session.StartRunning() took {0} ms");

			var perf4 = PerformanceCounter.Start ();

			var videoSettings = NSDictionary.FromObjectAndKey (new NSNumber ((int) CVPixelFormatType.CV32BGRA),
				CVPixelBuffer.PixelFormatTypeKey);


			// create a VideoDataOutput and add it to the sesion
			output = new AVCaptureVideoDataOutput {
				WeakVideoSettings = videoSettings
			};

			// configure the output
			queue = new DispatchQueue("ZxingScannerView"); // (Guid.NewGuid().ToString());

			var barcodeReader = new BarcodeReader(null, (img) => 	
			{
				var src = new RGBLuminanceSource(img); //, bmp.Width, bmp.Height);

				//Don't try and rotate properly if we're autorotating anyway
				if (ScanningOptions.AutoRotate.HasValue && ScanningOptions.AutoRotate.Value)
					return src;

				var tmpInterfaceOrientation = UIInterfaceOrientation.Portrait;
				InvokeOnMainThread(() => tmpInterfaceOrientation = UIApplication.SharedApplication.StatusBarOrientation);

				switch (tmpInterfaceOrientation)
				{
					case UIInterfaceOrientation.Portrait:
						return src.rotateCounterClockwise().rotateCounterClockwise().rotateCounterClockwise();
					case UIInterfaceOrientation.PortraitUpsideDown:
						return src.rotateCounterClockwise().rotateCounterClockwise().rotateCounterClockwise();
					case UIInterfaceOrientation.LandscapeLeft:
						return src;
					case UIInterfaceOrientation.LandscapeRight:
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
					//var sw = new System.Diagnostics.Stopwatch();
					//sw.Start();

					var rs = barcodeReader.Decode(img);

					//sw.Stop();

					//Console.WriteLine("Decode Time: {0} ms", sw.ElapsedMilliseconds);

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

			PerformanceCounter.Stop(perf4, "PERF: SetupCamera Finished.  Took {0} ms.");

			session.AddOutput (output);
			//session.StartRunning ();


			var perf5 = PerformanceCounter.Start ();

			NSError err = null;
			if (captureDevice.LockForConfiguration(out err))
			{
				if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
					captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
				else if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.AutoFocus))
					captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;

				if (captureDevice.IsExposureModeSupported (AVCaptureExposureMode.ContinuousAutoExposure))
					captureDevice.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
				else if (captureDevice.IsExposureModeSupported(AVCaptureExposureMode.AutoExpose))
					captureDevice.ExposureMode = AVCaptureExposureMode.AutoExpose;

				if (captureDevice.IsWhiteBalanceModeSupported (AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance))
					captureDevice.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
				else if (captureDevice.IsWhiteBalanceModeSupported (AVCaptureWhiteBalanceMode.AutoWhiteBalance))
						captureDevice.WhiteBalanceMode = AVCaptureWhiteBalanceMode.AutoWhiteBalance;

				if (UIDevice.CurrentDevice.CheckSystemVersion (7, 0) && captureDevice.AutoFocusRangeRestrictionSupported)
					captureDevice.AutoFocusRangeRestriction = AVCaptureAutoFocusRangeRestriction.Near;

				if (captureDevice.FocusPointOfInterestSupported)
					captureDevice.FocusPointOfInterest = new PointF(0.5f, 0.5f);

				if (captureDevice.ExposurePointOfInterestSupported)
					captureDevice.ExposurePointOfInterest = new PointF (0.5f, 0.5f);

				captureDevice.UnlockForConfiguration();
			}
			else
				Console.WriteLine("Failed to Lock for Config: " + err.Description);

			PerformanceCounter.Stop (perf5, "PERF: Setup Focus in {0} ms.");
	
			return true;
		}

		public void DidRotate(UIInterfaceOrientation orientation)
		{
			ResizePreview (orientation);

			this.LayoutSubviews ();

			//			if (overlayView != null)
			//	overlayView.LayoutSubviews ();
		}

		public void ResizePreview (UIInterfaceOrientation orientation)
		{
			if (previewLayer == null)
				return;

			previewLayer.Frame = new CGRect (0, 0, this.Frame.Width, this.Frame.Height);

			if (previewLayer.RespondsToSelector (new Selector ("connection")))
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
					device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
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
				{
					if (sampleBuffer != null)
					{
						sampleBuffer.Dispose ();
						sampleBuffer = null;
					}
					return;
				}


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
					sampleBuffer = null;

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
					var bytesPerRow = pixelBuffer.BytesPerRow;
					var width = pixelBuffer.Width;
					var height = pixelBuffer.Height;
					var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;
					// Create a CGImage on the RGB colorspace from the configured parameter above
					using (var cs = CGColorSpace.CreateDeviceRGB ())
					using (var context = new CGBitmapContext (baseAddress, (int)width, (int)height, 8, (int)bytesPerRow, cs, (CGImageAlphaInfo) flags))
					using (var cgImage = context.ToImage ())
					{
						pixelBuffer.Unlock (0);

						img = new UIImage(cgImage);
					}
				}

				return img;
			}
		}
	
		#region IZXingScanner implementation
		public void StartScanning (MobileBarcodeScanningOptions options, Action<Result> callback)
		{
			if (!stopped)
				return;

			stopped = false;

			var perf = PerformanceCounter.Start ();

			Setup (this.Frame);

			this.options = options;
			this.resultCallback = callback;

			Console.WriteLine("StartScanning");

			this.InvokeOnMainThread(() => {
				if (!SetupCaptureSession())
				{
					//Setup 'simulated' view:
					Console.WriteLine("Capture Session FAILED");

				}

				if (Runtime.Arch == Arch.SIMULATOR)
				{
					var simView = new UIView(new CGRect(0, 0, this.Frame.Width, this.Frame.Height));
					simView.BackgroundColor = UIColor.LightGray;
					simView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
					this.InsertSubview(simView, 0);

				}
			});

			if (!analyzing)
				analyzing = true;

			PerformanceCounter.Stop(perf, "PERF: StartScanning() Took {0} ms.");

			var evt = this.OnScannerSetupComplete;
			if (evt != null)
				evt ();
		}
		public void StartScanning (Action<Result> callback)
		{
			StartScanning (new MobileBarcodeScanningOptions (), callback);
		}

		public void StopScanning()
		{
			if (overlayView != null) {
				if (overlayView is ZXingDefaultOverlayView)
					(overlayView as ZXingDefaultOverlayView).Destroy ();

				overlayView = null;
			}
				
			if (stopped)
				return;

			Console.WriteLine("Stopping...");

			if (outputRecorder != null)
				outputRecorder.CancelTokenSource.Cancel();
	
			//Try removing all existing outputs prior to closing the session
			try
			{
				while (session.Outputs.Length > 0)
					session.RemoveOutput (session.Outputs [0]);
			}
			catch { }

			//Try to remove all existing inputs prior to closing the session
			try
			{
				while (session.Inputs.Length > 0)
					session.RemoveInput (session.Inputs [0]);
			}
			catch { }

			if (session.Running)
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

