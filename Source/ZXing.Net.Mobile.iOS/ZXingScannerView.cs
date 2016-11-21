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

        public event Action OnCancelButtonPressed;

		public string CancelButtonText { get;set; }
		public string FlashButtonText { get;set; }

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		void Setup(CGRect frame)
		{
			var started = DateTime.UtcNow;

			if (overlayView != null)
				overlayView.RemoveFromSuperview ();

            if (UseCustomOverlayView)
                overlayView = CustomOverlayView;
            else {
                overlayView = new ZXingDefaultOverlayView (new CGRect (0, 0, this.Frame.Width, this.Frame.Height),
                    TopText, BottomText, CancelButtonText, FlashButtonText,
                    () => {
                        var evt = OnCancelButtonPressed;
                        if (evt != null)
                            evt();
                    }, ToggleTorch);
            }

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


			// //Framerate set here (15 fps)
			// if (UIDevice.CurrentDevice.CheckSystemVersion (7, 0))
			// {
			// 	var perf1 = PerformanceCounter.Start ();

			// 	NSError lockForConfigErr = null;

			// 	captureDevice.LockForConfiguration (out lockForConfigErr);
			// 	if (lockForConfigErr == null)
			// 	{
			// 		captureDevice.ActiveVideoMinFrameDuration = new CMTime (1, 10);
			// 		captureDevice.UnlockForConfiguration ();
			// 	}

			// 	PerformanceCounter.Stop (perf1, "PERF: ActiveVideoMinFrameDuration Took {0} ms");
			// }
   //          else
   //              previewLayer.Connection.VideoMinFrameDuration = new CMTime(1, 10);


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

			var barcodeReader = new BarcodeReaderiOS(null, (img) => 	
			{
				var src = new RGBLuminanceSourceiOS(img); //, bmp.Width, bmp.Height);

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
					return false;

				try
				{
					//var sw = new System.Diagnostics.Stopwatch();
					//sw.Start();

                    var rs = barcodeReader.Decode(img);

					//sw.Stop();

					//Console.WriteLine("Decode Time: {0} ms", sw.ElapsedMilliseconds);

                    if (rs != null) {
						resultCallback(rs);
                        return true;
                    }
				}
				catch (Exception ex)
				{
					Console.WriteLine("DECODE FAILED: " + ex);
				}

                return false;
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

			if (previewLayer.RespondsToSelector (new Selector ("connection")) && previewLayer.Connection != null)
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
            public OutputRecorder(MobileBarcodeScanningOptions options, Func<LuminanceSource, bool> handleImage) : base()
            {
                HandleImage = handleImage;
                this.options = options;
            }

			MobileBarcodeScanningOptions options;
            Func<LuminanceSource, bool> HandleImage;

			DateTime lastAnalysis = DateTime.MinValue;
			volatile bool working = false;
            volatile bool wasScanned = false;

			[Export ("captureOutput:didDropSampleBuffer:fromConnection:")]
			public void DidDropSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				//Console.WriteLine("Dropped Sample Buffer");
			}

			public CancellationTokenSource CancelTokenSource = new CancellationTokenSource();


			public override void DidOutputSampleBuffer (AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
                var msSinceLastPreview = (DateTime.UtcNow - lastAnalysis).TotalMilliseconds;
                    
                if (msSinceLastPreview < options.DelayBetweenAnalyzingFrames
                    || (wasScanned && msSinceLastPreview < options.DelayBetweenContinuousScans)
                    || working
				    || CancelTokenSource.IsCancellationRequested)
				{
					if (sampleBuffer != null)
					{
						sampleBuffer.Dispose ();
						sampleBuffer = null;
					}
					return;
				}

                wasScanned = false;
				working = true;
				lastAnalysis = DateTime.UtcNow;

				try 
				{
                    // Get the CoreVideo image
                    using (var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer)
                    {
                        // Lock the base address
                        pixelBuffer.Lock(CVPixelBufferLock.ReadOnly); // MAYBE NEEDS READ/WRITE

                        CVPixelBufferARGB32LuminanceSource luminanceSource;

                        // Let's access the raw underlying data and create a luminance source from it
                        unsafe
                        {
                            var rawData = (byte*)pixelBuffer.BaseAddress.ToPointer();
                            var rawDatalen = (int)(pixelBuffer.Height * pixelBuffer.Width * 4); //This drops 8 bytes from the original length to give us the expected length

                            luminanceSource = new CVPixelBufferARGB32LuminanceSource(rawData, rawDatalen, (int)pixelBuffer.Width, (int)pixelBuffer.Height);
                        }

                        HandleImage(luminanceSource);

                        pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
                    }

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
		}
	
		#region IZXingScanner implementation
        public void StartScanning (Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
		{
			if (!stopped)
				return;

			stopped = false;

			var perf = PerformanceCounter.Start ();

			Setup (this.Frame);

			this.options = options ?? MobileBarcodeScanningOptions.Default;
			this.resultCallback = scanResultHandler;

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

		public void Torch (bool on)
		{
			try
			{
				NSError err;

				var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);

                if (device.HasTorch || device.HasFlash) {
                    
    				device.LockForConfiguration(out err);

    				if (on)
    				{
                        if (device.HasTorch)
    					    device.TorchMode = AVCaptureTorchMode.On;
                        if (device.HasFlash)
    					    device.FlashMode = AVCaptureFlashMode.On;
    				}
    				else
    				{
                        if (device.HasTorch)
    					    device.TorchMode = AVCaptureTorchMode.Off;
                        if (device.HasFlash)
    					    device.FlashMode = AVCaptureFlashMode.Off;
    				}

    				device.UnlockForConfiguration();
                }
				device = null;


				torch = on;
			}
			catch { }
		}

		public void ToggleTorch()
		{
			Torch(!IsTorchOn);
		}

		public void AutoFocus ()
		{
			//Doesn't do much on iOS :(
		}

        public void AutoFocus (int x, int y)
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

        bool? hasTorch = null;
        public bool HasTorch {
            get {
                if (hasTorch.HasValue)
                    return hasTorch.Value;

                var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
                hasTorch = device.HasFlash || device.HasTorch;
                return hasTorch.Value;
            }
        }
		#endregion
	}
}

