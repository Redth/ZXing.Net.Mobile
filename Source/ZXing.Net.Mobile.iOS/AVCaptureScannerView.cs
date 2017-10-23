using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using ObjCRuntime;
using UIKit;

using ZXing.Common;
using ZXing.Mobile;

namespace ZXing.Mobile
{
    public class AVCaptureScannerView : UIView, IZXingScanner<UIView>, IScannerSessionHost
	{
		public AVCaptureScannerView()
		{
		}

		public AVCaptureScannerView(IntPtr handle) : base(handle)
		{
		}

		public AVCaptureScannerView (CGRect frame) : base(frame)
		{
		}

		AVCaptureSession session;
		AVCaptureVideoPreviewLayer previewLayer;
		//AVCaptureVideoDataOutput output;
		//OutputRecorder outputRecorder;
		//DispatchQueue queue;
		Action<ZXing.Result> resultCallback;
		volatile bool stopped = true;

		volatile bool foundResult = false;
		CaptureDelegate captureDelegate;

		UIView layerView;
		UIView overlayView = null;

        public event Action OnCancelButtonPressed;

		public string CancelButtonText { get;set; }
		public string FlashButtonText { get;set; }

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }

		void Setup()
		{
			if (overlayView != null)
				overlayView.RemoveFromSuperview ();

            if (UseCustomOverlayView && CustomOverlayView != null)
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
				/*				UITapGestureRecognizer tapGestureRecognizer = new UITapGestureRecognizer ();

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
		}


		bool torch = false;
		bool analyzing = true;
        DateTime lastAnalysis = DateTime.UtcNow.AddYears (-99);
        bool wasScanned = false;
        bool working = false;


		bool SetupCaptureSession ()
		{
            var availableResolutions = new List<CameraResolution> ();

            var consideredResolutions = new Dictionary<NSString, CameraResolution> {
                { AVCaptureSession.Preset352x288, new CameraResolution   { Width = 352,  Height = 288 } },
                { AVCaptureSession.PresetMedium, new CameraResolution    { Width = 480,  Height = 360 } },  //480x360
                { AVCaptureSession.Preset640x480, new CameraResolution   { Width = 640,  Height = 480 } },
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
				if (ScanningOptions.UseFrontCameraIfAvailable.HasValue &&
					ScanningOptions.UseFrontCameraIfAvailable.Value &&
					device.Position == AVCaptureDevicePosition.Front)

					break; //Front camera successfully set
				else if (device.Position == AVCaptureDevicePosition.Back && (!ScanningOptions.UseFrontCameraIfAvailable.HasValue || !ScanningOptions.UseFrontCameraIfAvailable.Value))
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

            resolution = ScanningOptions.GetResolution (availableResolutions);

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


			foundResult = false;
			//Detect barcodes with built in avcapture stuff
			AVCaptureMetadataOutput metadataOutput = new AVCaptureMetadataOutput();

			captureDelegate = new CaptureDelegate (metaDataObjects =>
				{
                    if (!analyzing)
                        return;

					//Console.WriteLine("Found MetaData Objects");

                    var msSinceLastPreview = (DateTime.UtcNow - lastAnalysis).TotalMilliseconds;

                    if (msSinceLastPreview < ScanningOptions.DelayBetweenAnalyzingFrames 
                        || (wasScanned && msSinceLastPreview < ScanningOptions.DelayBetweenContinuousScans)
                        || working)
                        //|| CancelTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    try {
                        working = true;
                        wasScanned = false;
                        lastAnalysis = DateTime.UtcNow;

                        var mdo = metaDataObjects.FirstOrDefault();

                        var readableObj = mdo as AVMetadataMachineReadableCodeObject;

                        if (readableObj == null)
                            return;

                        wasScanned = true;

                        var zxingFormat = ZXingBarcodeFormatFromAVCaptureBarcodeFormat(readableObj.Type.ToString());

                        var rs = new ZXing.Result(readableObj.StringValue, null, null, zxingFormat);

                        resultCallback(rs);
                    } finally {
                        working = false;
                    }
				});

			metadataOutput.SetDelegate (captureDelegate, DispatchQueue.MainQueue);
			session.AddOutput (metadataOutput);

			//Setup barcode formats
			if (ScanningOptions.PossibleFormats != null && ScanningOptions.PossibleFormats.Count > 0)
			{
                #if __UNIFIED__
                var formats = AVMetadataObjectType.None;

                foreach (var f in ScanningOptions.PossibleFormats)
                    formats |= AVCaptureBarcodeFormatFromZXingBarcodeFormat (f);
					
                formats &= ~AVMetadataObjectType.None;

                metadataOutput.MetadataObjectTypes = formats;
                #else
                var formats = new List<string> ();

                foreach (var f in ScanningOptions.PossibleFormats)
                    formats.AddRange (AVCaptureBarcodeFormatFromZXingBarcodeFormat (f));

                metadataOutput.MetadataObjectTypes = (from f in formats.Distinct () select new NSString(f)).ToArray();
                #endif
			}
			else
				metadataOutput.MetadataObjectTypes = metadataOutput.AvailableMetadataObjectTypes;


		


			previewLayer = new AVCaptureVideoPreviewLayer(session);

			// //Framerate set here (15 fps)
   //          if (previewLayer.RespondsToSelector(new Selector("connection")))
   //          {
			// 	if (UIDevice.CurrentDevice.CheckSystemVersion (7, 0))
			// 	{
			// 		var perf1 = PerformanceCounter.Start ();

			// 		NSError lockForConfigErr = null;

			// 		captureDevice.LockForConfiguration (out lockForConfigErr);
			// 		if (lockForConfigErr == null)
			// 		{
			// 			captureDevice.ActiveVideoMinFrameDuration = new CMTime (1, 10);
			// 			captureDevice.UnlockForConfiguration ();
			// 		}

			// 		PerformanceCounter.Stop (perf1, "PERF: ActiveVideoMinFrameDuration Took {0} ms");
			// 	}
   //              else
   //                  previewLayer.Connection.VideoMinFrameDuration = new CMTime(1, 10);
   //          }

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

			session.StartRunning ();

			Console.WriteLine ("RUNNING!!!");



			//output.AlwaysDiscardsLateVideoFrames = true;


			Console.WriteLine("SetupCamera Finished");

			//session.AddOutput (output);
			//session.StartRunning ();


			if (captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
			{
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
						captureDevice.FocusPointOfInterest = new CGPoint(0.5f, 0.5f);

					if (captureDevice.ExposurePointOfInterestSupported)
						captureDevice.ExposurePointOfInterest = new CGPoint (0.5f, 0.5f);

					captureDevice.UnlockForConfiguration();
				}
				else
					Console.WriteLine("Failed to Lock for Config: " + err.Description);
			}

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

			previewLayer.Frame = new CGRect(0, 0, this.Frame.Width, this.Frame.Height);

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

		public void Focus(CGPoint pointOfInterest)
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



		#region IZXingScanner implementation
        public void StartScanning (Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
		{
			if (!analyzing)
				analyzing = true;

			if (!stopped)
				return;

			Setup ();

			this.ScanningOptions = options;
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

			stopped = false;
		}
		

		public void StopScanning()
		{
			if (stopped)
				return;

			Console.WriteLine("Stopping...");

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
                if (device.HasFlash || device.HasTorch) {
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

		public static bool SupportsAllRequestedBarcodeFormats(IEnumerable<BarcodeFormat> formats)
		{
			var supported = new List<BarcodeFormat> () {
				BarcodeFormat.AZTEC, BarcodeFormat.CODE_128, BarcodeFormat.CODE_39,
				BarcodeFormat.CODE_93, BarcodeFormat.EAN_13, BarcodeFormat.EAN_8,
				BarcodeFormat.PDF_417, BarcodeFormat.QR_CODE, BarcodeFormat.UPC_E,
                BarcodeFormat.DATA_MATRIX, BarcodeFormat.ITF,
				BarcodeFormat.All_1D
			};

			return !formats.Any (f => !supported.Contains (f));
		}

		BarcodeFormat ZXingBarcodeFormatFromAVCaptureBarcodeFormat(string avMetadataObjectType)
		{
			switch(avMetadataObjectType)
			{
			case "AztecCode":
				return BarcodeFormat.AZTEC;
			case "Code128Code":
				return BarcodeFormat.CODE_128;
			case "Code39Code":
				return BarcodeFormat.CODE_39;
			case "Code39Mod43Code":
				return BarcodeFormat.CODE_39;
			case "Code93Code":
				return BarcodeFormat.CODE_93;
			case "EAN13Code":
				return BarcodeFormat.EAN_13;
			case "EAN8Code":
				return BarcodeFormat.EAN_8;
			case "PDF417Code":
				return BarcodeFormat.PDF_417;
			case "QRCode":
				return BarcodeFormat.QR_CODE;
			case "UPCECode":
				return BarcodeFormat.UPC_E;
			case "DataMatrixCode":
				return BarcodeFormat.DATA_MATRIX;
			case "Interleaved2of5Code":
				return BarcodeFormat.ITF;
			default:
				return BarcodeFormat.QR_CODE;
			}		    
		}

        #if __UNIFIED__
		AVMetadataObjectType AVCaptureBarcodeFormatFromZXingBarcodeFormat(BarcodeFormat zxingBarcodeFormat)
		{
            AVMetadataObjectType formats = AVMetadataObjectType.None;

			switch (zxingBarcodeFormat)
			{
            case BarcodeFormat.AZTEC:
                formats |= AVMetadataObjectType.AztecCode;					
                break;
            case BarcodeFormat.CODE_128:
                formats |= AVMetadataObjectType.Code128Code;
				break;
			case BarcodeFormat.CODE_39:
                formats |= AVMetadataObjectType.Code39Code;
                formats |= AVMetadataObjectType.Code39Mod43Code;				
				break;
			case BarcodeFormat.CODE_93:
                formats |= AVMetadataObjectType.Code93Code;
				break;
			case BarcodeFormat.EAN_13:
                formats |= AVMetadataObjectType.EAN13Code;
				break;
			case BarcodeFormat.EAN_8:
                formats |= AVMetadataObjectType.EAN8Code;
				break;
			case BarcodeFormat.PDF_417:
                formats |= AVMetadataObjectType.PDF417Code;
				break;
			case BarcodeFormat.QR_CODE:
                formats |= AVMetadataObjectType.QRCode;
				break;
			case BarcodeFormat.UPC_E:
                formats |= AVMetadataObjectType.UPCECode;
				break;
			case BarcodeFormat.All_1D:
                formats |= AVMetadataObjectType.UPCECode;
                formats |= AVMetadataObjectType.EAN13Code;
                formats |= AVMetadataObjectType.EAN8Code;
                formats |= AVMetadataObjectType.Code39Code;
                formats |= AVMetadataObjectType.Code39Mod43Code;
                formats |= AVMetadataObjectType.Code93Code;
				break;			
            case BarcodeFormat.DATA_MATRIX:
                formats |= AVMetadataObjectType.DataMatrixCode;
                break;
            case BarcodeFormat.ITF:
                formats |= AVMetadataObjectType.ITF14Code;
                break;
            case BarcodeFormat.CODABAR:
            case BarcodeFormat.MAXICODE:
			case BarcodeFormat.MSI:
			case BarcodeFormat.PLESSEY:
			case BarcodeFormat.RSS_14:
			case BarcodeFormat.RSS_EXPANDED:
			case BarcodeFormat.UPC_A:
				//TODO: Throw exception?
				break;
			}

            return formats;
		}
        #else
        string[] AVCaptureBarcodeFormatFromZXingBarcodeFormat(BarcodeFormat zxingBarcodeFormat)
        {
            List<string> formats = new List<string> ();

            switch (zxingBarcodeFormat)
            {
            case BarcodeFormat.AZTEC:
                formats.Add (AVMetadataObject.TypeAztecCode);
                break;
            case BarcodeFormat.CODE_128:
                formats.Add (AVMetadataObject.TypeCode128Code);
                break;
            case BarcodeFormat.CODE_39:
                formats.Add (AVMetadataObject.TypeCode39Code);
                formats.Add (AVMetadataObject.TypeCode39Mod43Code);
                break;
            case BarcodeFormat.CODE_93:
                formats.Add (AVMetadataObject.TypeCode93Code);
                break;
            case BarcodeFormat.EAN_13:
                formats.Add (AVMetadataObject.TypeEAN13Code);
                break;
            case BarcodeFormat.EAN_8:
                formats.Add (AVMetadataObject.TypeEAN8Code);
                break;
            case BarcodeFormat.PDF_417:
                formats.Add (AVMetadataObject.TypePDF417Code);
                break;
            case BarcodeFormat.QR_CODE:
                formats.Add (AVMetadataObject.TypeQRCode);
                break;
            case BarcodeFormat.UPC_E:
                formats.Add (AVMetadataObject.TypeUPCECode);
                break;
            case BarcodeFormat.All_1D:
                formats.Add (AVMetadataObject.TypeUPCECode);
                formats.Add (AVMetadataObject.TypeEAN13Code);
                formats.Add (AVMetadataObject.TypeEAN8Code);
                formats.Add (AVMetadataObject.TypeCode39Code);
                formats.Add (AVMetadataObject.TypeCode39Mod43Code);
                formats.Add (AVMetadataObject.TypeCode93Code);
                break;
            case BarcodeFormat.DATA_MATRIX:
                formats.Add (AVMetadataObject.TypeDataMatrixCode);
                break;
            case BarcodeFormat.ITF:
                formats.Add (AVMetadataObject.TypeITF14Code);
                break;
            case BarcodeFormat.CODABAR:                        
            case BarcodeFormat.MAXICODE:
            case BarcodeFormat.MSI:
            case BarcodeFormat.PLESSEY:
            case BarcodeFormat.RSS_14:
            case BarcodeFormat.RSS_EXPANDED:
            case BarcodeFormat.UPC_A:
                //TODO: Throw exception?
                break;
            }

            return formats.ToArray();
        }
        #endif
	}




	class CaptureDelegate : AVCaptureMetadataOutputObjectsDelegate 
	{
		public CaptureDelegate (Action<IEnumerable<AVMetadataObject>> onCapture)
		{
			OnCapture = onCapture;
		}

		public Action<IEnumerable<AVMetadataObject>> OnCapture { get;set; }

		public override void DidOutputMetadataObjects (AVCaptureMetadataOutput captureOutput, AVMetadataObject[] metadataObjects, AVCaptureConnection connection)
		{
			if (OnCapture != null && metadataObjects != null)
				OnCapture (metadataObjects);
		}
	}
}

