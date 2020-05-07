using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using CoreFoundation;
using AVFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using ObjCRuntime;
using UIKit;

using ZXing.Common;
using ZXing.Mobile;

namespace ZXing.Mobile
{
	public class ZXingScannerView : UIView, IZXingScanner<UIView>, IScannerSessionHost
	{
		public delegate void ScannerSetupCompleteDelegate();
		public event ScannerSetupCompleteDelegate OnScannerSetupComplete;

		public ZXingScannerView()
		{
		}

		public ZXingScannerView(IntPtr handle) : base(handle)
		{
		}

		public ZXingScannerView(CGRect frame) : base(frame)
		{
		}

		AVCaptureSession session;
		AVCaptureDevice captureDevice = null;
		AVCaptureVideoPreviewLayer previewLayer;
		AVCaptureVideoDataOutput output;
		OutputRecorder outputRecorder;
		DispatchQueue queue;
		Action<ZXing.Result> resultCallback;
		volatile bool stopped = true;

		UIView layerView;
		UIView overlayView = null;

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public event Action OnCancelButtonPressed;

		public string CancelButtonText { get; set; }
		public string FlashButtonText { get; set; }

		bool shouldRotatePreviewBuffer = false;

		AVConfigs captureDeviceOriginalConfig;

		void Setup()
		{
			var started = DateTime.UtcNow;

			if (overlayView != null)
				overlayView.RemoveFromSuperview();

			if (UseCustomOverlayView)
				overlayView = CustomOverlayView;
			else
			{
				overlayView = new ZXingDefaultOverlayView(new CGRect(0, 0, Frame.Width, Frame.Height),
					TopText, BottomText, CancelButtonText, FlashButtonText,
					() =>
					{
						var evt = OnCancelButtonPressed;
						if (evt != null)
							evt();
					}, ToggleTorch);
			}

			if (overlayView != null)
			{
				overlayView.Frame = new CGRect(0, 0, this.Frame.Width, this.Frame.Height);
				overlayView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			}

			var total = DateTime.UtcNow - started;
			Console.WriteLine("ZXingScannerView.Setup() took {0} ms.", total.TotalMilliseconds);
		}


		bool torch = false;
		bool analyzing = true;

		bool SetupCaptureSession()
		{
			var started = DateTime.UtcNow;

			var availableResolutions = new List<CameraResolution>();

			var consideredResolutions = new Dictionary<NSString, CameraResolution> {
				{ AVCaptureSession.Preset352x288, new CameraResolution   { Width = 352,  Height = 288 } },
				{ AVCaptureSession.PresetMedium, new CameraResolution    { Width = 480,  Height = 360 } },	//480x360
				{ AVCaptureSession.Preset640x480, new CameraResolution   { Width = 640,  Height = 480 } },
				{ AVCaptureSession.Preset1280x720, new CameraResolution  { Width = 1280, Height = 720 } },
				{ AVCaptureSession.Preset1920x1080, new CameraResolution { Width = 1920, Height = 1080 } }
			};

			// configure the capture session for low resolution, change this if your code
			// can cope with more data or volume
			session = new AVCaptureSession()
			{
				SessionPreset = AVCaptureSession.Preset640x480
			};

			// create a device input and attach it to the session
			var devices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);
			foreach (var device in devices)
			{
				captureDevice = device;
				if (ScanningOptions.UseFrontCameraIfAvailable.HasValue &&
					ScanningOptions.UseFrontCameraIfAvailable.Value &&
					device.Position == AVCaptureDevicePosition.Front)

					break; //Front camera successfully set
				else if (device.Position == AVCaptureDevicePosition.Back && (!ScanningOptions.UseFrontCameraIfAvailable.HasValue || !ScanningOptions.UseFrontCameraIfAvailable.Value))
					break; //Back camera succesfully set
			}
			if (captureDevice == null)
			{
				Console.WriteLine("No captureDevice - this won't work on the simulator, try a physical device");
				if (overlayView != null)
				{
					AddSubview(overlayView);
					BringSubviewToFront(overlayView);
				}
				return false;
			}

			CameraResolution resolution = null;

			// Find resolution
			// Go through the resolutions we can even consider
			foreach (var cr in consideredResolutions)
			{
				// Now check to make sure our selected device supports the resolution
				// so we can add it to the list to pick from
				if (captureDevice.SupportsAVCaptureSessionPreset(cr.Key))
					availableResolutions.Add(cr.Value);
			}

			resolution = ScanningOptions.GetResolution(availableResolutions);

			// See if the user selected a resolution
			if (resolution != null)
			{
				// Now get the preset string from the resolution chosen
				var preset = (from c in consideredResolutions
							  where c.Value.Width == resolution.Width
								&& c.Value.Height == resolution.Height
							  select c.Key).FirstOrDefault();

				// If we found a matching preset, let's set it on the session
				if (!string.IsNullOrEmpty(preset))
					session.SessionPreset = preset;
			}

			var input = AVCaptureDeviceInput.FromDevice(captureDevice);
			if (input == null)
			{
				Console.WriteLine("No input - this won't work on the simulator, try a physical device");
				if (overlayView != null)
				{
					AddSubview(overlayView);
					BringSubviewToFront(overlayView);
				}
				return false;
			}
			else
				session.AddInput(input);


			var startedAVPreviewLayerAlloc = PerformanceCounter.Start();

			previewLayer = new AVCaptureVideoPreviewLayer(session);

			PerformanceCounter.Stop(startedAVPreviewLayerAlloc, "Alloc AVCaptureVideoPreviewLayer took {0} ms.");

			var perf2 = PerformanceCounter.Start();

			previewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			previewLayer.Frame = new CGRect(0, 0, Frame.Width, Frame.Height);
			previewLayer.Position = new CGPoint(Layer.Bounds.Width / 2, (Layer.Bounds.Height / 2));

			layerView = new UIView(new CGRect(0, 0, Frame.Width, Frame.Height));
			layerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			layerView.Layer.AddSublayer(previewLayer);

			AddSubview(layerView);

			ResizePreview(UIApplication.SharedApplication.StatusBarOrientation);

			if (overlayView != null)
			{
				AddSubview(overlayView);
				BringSubviewToFront(overlayView);
			}

			PerformanceCounter.Stop(perf2, "PERF: Setting up layers took {0} ms");

			var perf3 = PerformanceCounter.Start();

			session.StartRunning();

			PerformanceCounter.Stop(perf3, "PERF: session.StartRunning() took {0} ms");

			var perf4 = PerformanceCounter.Start();

			var videoSettings = NSDictionary.FromObjectAndKey(new NSNumber((int)CVPixelFormatType.CV32BGRA),
				CVPixelBuffer.PixelFormatTypeKey);

			// create a VideoDataOutput and add it to the sesion
			output = new AVCaptureVideoDataOutput
			{
				WeakVideoSettings = videoSettings
			};

			// configure the output
			queue = new DispatchQueue("ZxingScannerView"); // (Guid.NewGuid().ToString());

			var barcodeReader = ScanningOptions.BuildBarcodeReader();

			outputRecorder = new OutputRecorder(this, img =>
			{
				var ls = img;

				if (!IsAnalyzing)
					return false;

				try
				{
					var perfDecode = PerformanceCounter.Start();

					if (shouldRotatePreviewBuffer)
						ls = ls.rotateCounterClockwise();

					var result = barcodeReader.Decode(ls);

					PerformanceCounter.Stop(perfDecode, "Decode Time: {0} ms");

					if (result != null)
					{
						resultCallback(result);
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
			output.SetSampleBufferDelegate(outputRecorder, queue);

			PerformanceCounter.Stop(perf4, "PERF: SetupCamera Finished.  Took {0} ms.");

			session.AddOutput(output);

			var perf5 = PerformanceCounter.Start();

			if (captureDevice.LockForConfiguration(out var err))
			{
				captureDeviceOriginalConfig = new AVConfigs
				{
					FocusMode = captureDevice.FocusMode,
					ExposureMode = captureDevice.ExposureMode,
					WhiteBalanceMode = captureDevice.WhiteBalanceMode,
					AutoFocusRangeRestriction = captureDevice.AutoFocusRangeRestriction,
				};

				if (captureDevice.HasFlash)
					captureDeviceOriginalConfig.FlashMode = captureDevice.FlashMode;
				if (captureDevice.HasTorch)
					captureDeviceOriginalConfig.TorchMode = captureDevice.TorchMode;
				if (captureDevice.FocusPointOfInterestSupported)
					captureDeviceOriginalConfig.FocusPointOfInterest = captureDevice.FocusPointOfInterest;
				if (captureDevice.ExposurePointOfInterestSupported)
					captureDeviceOriginalConfig.ExposurePointOfInterest = captureDevice.ExposurePointOfInterest;

				if (ScanningOptions.DisableAutofocus)
				{
					captureDevice.FocusMode = AVCaptureFocusMode.Locked;
				}
				else
				{
					if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
						captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
					else if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.AutoFocus))
						captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
				}

				if (captureDevice.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
					captureDevice.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
				else if (captureDevice.IsExposureModeSupported(AVCaptureExposureMode.AutoExpose))
					captureDevice.ExposureMode = AVCaptureExposureMode.AutoExpose;

				if (captureDevice.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance))
					captureDevice.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
				else if (captureDevice.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.AutoWhiteBalance))
					captureDevice.WhiteBalanceMode = AVCaptureWhiteBalanceMode.AutoWhiteBalance;

				if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0) && captureDevice.AutoFocusRangeRestrictionSupported)
				{
					captureDevice.AutoFocusRangeRestriction = AVCaptureAutoFocusRangeRestriction.Near;
				}

				if (captureDevice.FocusPointOfInterestSupported)
					captureDevice.FocusPointOfInterest = new PointF(0.5f, 0.5f);

				if (captureDevice.ExposurePointOfInterestSupported)
					captureDevice.ExposurePointOfInterest = new PointF(0.5f, 0.5f);

				captureDevice.UnlockForConfiguration();
			}
			else
				Console.WriteLine("Failed to Lock for Config: " + err.Description);

			PerformanceCounter.Stop(perf5, "PERF: Setup Focus in {0} ms.");

			return true;
		}

		public void DidRotate(UIInterfaceOrientation orientation)
		{
			ResizePreview(orientation);

			LayoutSubviews();
		}

		public void ResizePreview(UIInterfaceOrientation orientation)
		{
			shouldRotatePreviewBuffer = orientation == UIInterfaceOrientation.Portrait || orientation == UIInterfaceOrientation.PortraitUpsideDown;

			if (previewLayer == null)
				return;

			previewLayer.Frame = new CGRect(0, 0, Frame.Width, Frame.Height);

			if (previewLayer.RespondsToSelector(new Selector("connection")) && previewLayer.Connection != null)
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
			public OutputRecorder(IScannerSessionHost scannerHost, Func<LuminanceSource, bool> handleImage) : base()
			{
				this.handleImage = handleImage;
				this.scannerHost = scannerHost;
			}

			IScannerSessionHost scannerHost;
			Func<LuminanceSource, bool> handleImage;

			DateTime lastAnalysis = DateTime.MinValue;
			volatile bool working = false;
			volatile bool wasScanned = false;

			[Export("captureOutput:didDropSampleBuffer:fromConnection:")]
			public override void DidDropSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
			}

			public CancellationTokenSource CancelTokenSource = new CancellationTokenSource();


			public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				var msSinceLastPreview = (DateTime.UtcNow - lastAnalysis).TotalMilliseconds;

				if (msSinceLastPreview < scannerHost.ScanningOptions.DelayBetweenAnalyzingFrames
					|| (wasScanned && msSinceLastPreview < scannerHost.ScanningOptions.DelayBetweenContinuousScans)
					|| working
					|| CancelTokenSource.IsCancellationRequested)
				{

					if (msSinceLastPreview < scannerHost.ScanningOptions.DelayBetweenAnalyzingFrames)
						Console.WriteLine("Too soon between frames");
					if (wasScanned && msSinceLastPreview < scannerHost.ScanningOptions.DelayBetweenContinuousScans)
						Console.WriteLine("Too soon since last scan");

					if (sampleBuffer != null)
					{
						sampleBuffer.Dispose();
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

						LuminanceSource luminanceSource;

						// Let's access the raw underlying data and create a luminance source from it
						unsafe
						{
							var rawData = (byte*)pixelBuffer.BaseAddress.ToPointer();
							var rawDatalen = (int)(pixelBuffer.Height * pixelBuffer.Width * 4); //This drops 8 bytes from the original length to give us the expected length

							luminanceSource = new CVPixelBufferBGRA32LuminanceSource(rawData, rawDatalen, (int)pixelBuffer.Width, (int)pixelBuffer.Height);
						}

						if (handleImage(luminanceSource))
							wasScanned = true;

						pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
					}

					//
					// Although this looks innocent "Oh, he is just optimizing this case away"
					// this is incredibly important to call on this callback, because the AVFoundation
					// has a fixed number of buffers and if it runs out of free buffers, it will stop
					// delivering frames. 
					//	
					sampleBuffer.Dispose();
					sampleBuffer = null;

				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				finally
				{
					working = false;
				}

			}
		}

		#region IZXingScanner implementation
		public void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
		{
			if (!stopped)
				return;

			stopped = false;

			var perf = PerformanceCounter.Start();

			Setup();

			ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;
			resultCallback = scanResultHandler;

			Console.WriteLine("StartScanning");

			InvokeOnMainThread(() =>
			{
				if (!SetupCaptureSession())
				{
					//Setup 'simulated' view:
					Console.WriteLine("Capture Session FAILED");
				}

				if (Runtime.Arch == Arch.SIMULATOR)
				{
					InsertSubview(new UIView(new CGRect(0, 0, this.Frame.Width, this.Frame.Height))
					{
						BackgroundColor = UIColor.LightGray,
						AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
					}, 0);

				}
			});

			if (!analyzing)
				analyzing = true;

			PerformanceCounter.Stop(perf, "PERF: StartScanning() Took {0} ms.");

			OnScannerSetupComplete?.Invoke();
		}

		public void StopScanning()
		{
			if (overlayView != null)
			{
				if (overlayView is ZXingDefaultOverlayView)
					(overlayView as ZXingDefaultOverlayView).Destroy();

				overlayView = null;
			}

			if (stopped)
				return;

			Console.WriteLine("Stopping...");

			if (outputRecorder != null)
				outputRecorder.CancelTokenSource.Cancel();

			// Revert camera settings to original
			if (captureDevice != null && captureDevice.LockForConfiguration(out var err))
			{
				captureDevice.FocusMode = captureDeviceOriginalConfig.FocusMode;
				captureDevice.ExposureMode = captureDeviceOriginalConfig.ExposureMode;
				captureDevice.WhiteBalanceMode = captureDeviceOriginalConfig.WhiteBalanceMode;

				if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0) && captureDevice.AutoFocusRangeRestrictionSupported)
					captureDevice.AutoFocusRangeRestriction = captureDeviceOriginalConfig.AutoFocusRangeRestriction;

				if (captureDevice.FocusPointOfInterestSupported)
					captureDevice.FocusPointOfInterest = captureDeviceOriginalConfig.FocusPointOfInterest;

				if (captureDevice.ExposurePointOfInterestSupported)
					captureDevice.ExposurePointOfInterest = captureDeviceOriginalConfig.ExposurePointOfInterest;

				if (captureDevice.HasFlash)
					captureDevice.FlashMode = captureDeviceOriginalConfig.FlashMode;
				if (captureDevice.HasTorch)
					captureDevice.TorchMode = captureDeviceOriginalConfig.TorchMode;

				captureDevice.UnlockForConfiguration();
			}

			//Try removing all existing outputs prior to closing the session
			try
			{
				while (session.Outputs.Length > 0)
					session.RemoveOutput(session.Outputs[0]);
			}
			catch { }

			//Try to remove all existing inputs prior to closing the session
			try
			{
				while (session.Inputs.Length > 0)
					session.RemoveInput(session.Inputs[0]);
			}
			catch { }

			if (session.Running)
				session.StopRunning();

			stopped = true;
		}

		public void PauseAnalysis()
			=> analyzing = false;

		public void ResumeAnalysis()
			=> analyzing = true;

		public void Torch(bool on)
		{
			try
			{
				var device = captureDevice ?? AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
				if (device != null && (device.HasTorch || device.HasFlash))
				{
					device.LockForConfiguration(out var err);

					if (err == null)
					{
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
					}

					try
					{
						device.UnlockForConfiguration();
					}
					catch { }
				}

				torch = on;
			}
			catch { }
		}

		public void ToggleTorch()
			=> Torch(!IsTorchOn);

		public void AutoFocus()
		{
			//Doesn't do much on iOS :(
		}

		public void AutoFocus(int x, int y)
		{
			//Doesn't do much on iOS :(
		}

		public string TopText { get; set; }
		public string BottomText { get; set; }


		public UIView CustomOverlayView { get; set; }
		public bool UseCustomOverlayView { get; set; }
		public bool IsAnalyzing { get { return analyzing; } }
		public bool IsTorchOn { get { return torch; } }

		bool? hasTorch = null;
		public bool HasTorch
		{
			get
			{
				if (hasTorch.HasValue)
					return hasTorch.Value;

				var device = captureDevice ?? AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
				hasTorch = device.HasFlash || device.HasTorch;
				return hasTorch.Value;
			}
		}
		#endregion
	}

	struct AVConfigs
	{
		public AVCaptureFocusMode FocusMode;
		public AVCaptureExposureMode ExposureMode;
		public AVCaptureWhiteBalanceMode WhiteBalanceMode;
		public AVCaptureAutoFocusRangeRestriction AutoFocusRangeRestriction;
		public CGPoint FocusPointOfInterest;
		public CGPoint ExposurePointOfInterest;
		public AVCaptureFlashMode FlashMode;
		public AVCaptureTorchMode TorchMode;
	}
}
