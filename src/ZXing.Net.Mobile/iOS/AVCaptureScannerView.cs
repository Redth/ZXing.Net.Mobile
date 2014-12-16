using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if __UNIFIED__
using Foundation;
using AVFoundation;
using CoreFoundation;
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
using System.Drawing;

using CGRect = global::System.Drawing.RectangleF;
using CGPoint = global::System.Drawing.PointF;
#endif

using ZXing.Common;
using ZXing.Mobile;

namespace ZXing.Mobile
{
	public class AVCaptureScannerView : UIView, IZXingScanner<UIView>
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
		//BarcodeReader barcodeReader;

		volatile bool foundResult = false;

		UIView layerView;
		UIView overlayView = null;

		public string CancelButtonText { get;set; }
		public string FlashButtonText { get;set; }

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		void Setup()
		{
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


		bool SetupCaptureSession ()
		{
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

			var dg = new CaptureDelegate (metaDataObjects =>
				{
					if (foundResult)
						return;

					//Console.WriteLine("Found MetaData Objects");

					var mdo = metaDataObjects.FirstOrDefault();

					if (mdo == null)
						return;

					var readableObj = mdo as AVMetadataMachineReadableCodeObject;

					if (readableObj == null)
						return;

					foundResult = true;

					//Console.WriteLine("Barcode: " + readableObj.StringValue);

					var zxingFormat = ZXingBarcodeFormatFromAVCaptureBarcodeFormat(readableObj.Type.ToString());

					var rs = new ZXing.Result(readableObj.StringValue, null, null, zxingFormat);

					resultCallback(rs);
				});

			metadataOutput.SetDelegate (dg, DispatchQueue.MainQueue);
			session.AddOutput (metadataOutput);

			//Setup barcode formats
			if (ScanningOptions.PossibleFormats != null && ScanningOptions.PossibleFormats.Count > 0)
			{
                var formats = AVMetadataObjectType.None;

                foreach (var f in ScanningOptions.PossibleFormats)
                    formats |= AVCaptureBarcodeFormatFromZXingBarcodeFormat (f);
					
                formats &= ~AVMetadataObjectType.None;

                metadataOutput.MetadataObjectTypes = formats;
			}
			else
				metadataOutput.MetadataObjectTypes = metadataOutput.AvailableMetadataObjectTypes;


		


			previewLayer = new AVCaptureVideoPreviewLayer(session);

			//Framerate set here (15 fps)
            if (previewLayer.RespondsToSelector(new Selector("connection")))
            {
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
            }

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
		public void StartScanning (MobileBarcodeScanningOptions options, Action<Result> callback)
		{
			if (!analyzing)
				analyzing = true;

			if (!stopped)
				return;

			Setup ();

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
			if (avMetadataObjectType == AVMetadataObject.TypeAztecCode)
				return BarcodeFormat.AZTEC;
			if (avMetadataObjectType == AVMetadataObject.TypeCode128Code)
				return BarcodeFormat.CODE_128;
			if (avMetadataObjectType == AVMetadataObject.TypeCode39Code)
				return BarcodeFormat.CODE_39;
			if (avMetadataObjectType == AVMetadataObject.TypeCode39Mod43Code)
				return BarcodeFormat.CODE_39;
			if (avMetadataObjectType == AVMetadataObject.TypeCode93Code)
				return BarcodeFormat.CODE_93;
			if (avMetadataObjectType == AVMetadataObject.TypeEAN13Code)
				return BarcodeFormat.EAN_13;
			if (avMetadataObjectType == AVMetadataObject.TypeEAN8Code)
				return BarcodeFormat.EAN_8;
			if (avMetadataObjectType == AVMetadataObject.TypePDF417Code)
				return BarcodeFormat.PDF_417;
			if (avMetadataObjectType == AVMetadataObject.TypeQRCode)
				return BarcodeFormat.QR_CODE;
			if (avMetadataObjectType == AVMetadataObject.TypeUPCECode)
				return BarcodeFormat.UPC_E;

			return BarcodeFormat.QR_CODE;
		}

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

