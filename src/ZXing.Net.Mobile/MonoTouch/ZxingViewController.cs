using System;
using System.Collections;
using System.Resources;
using System.Runtime.InteropServices;
using MonoTouch.UIKit;
using MonoTouch.CoreFoundation;
using MonoTouch.AVFoundation;
using MonoTouch.CoreVideo;
using MonoTouch.CoreMedia;
using ZXing;
using ZXing.Common;

namespace ZXing.Mobile
{
	// based on https://github.com/xamarin/monotouch-samples/blob/master/AVCaptureFrames/Main.cs
	public class ZxingViewController : UIViewController
	{
		public ZxingViewController(MobileBarcodeScanningOptions options, bool showButtons, bool showOverlay) : base()
		{
			this.Options = options;
			this.ShowButtons = showButtons;
			this.ShowOverlay = showOverlay;
		}

		public event Action<Result> Scan;

		AVCaptureSession session;
		AVCaptureVideoPreviewLayer previewLayer;
		AVCaptureDevice captureDevice;

		//UIImageView overlayView;
		UIView overlayView;
		UIButton buttonCancel;
		UIButton buttonFlash;

		DispatchQueue queue;
		ZxingScanner scanner;

		public bool ShowButtons { get; private set; }
		public bool ShowOverlay { get; private set; }

		public MobileBarcodeScanningOptions Options { get; private set; }

		public override void LoadView ()
		{
			base.LoadView ();

			if (!SetupCaptureSession ())
				throw new NotSupportedException (scanner);

			previewLayer.Frame = UIScreen.MainScreen.Bounds;
			View.Layer.AddSublayer (previewLayer);
		}

		public override void ViewDidLoad ()
		{
			if (ShowOverlay)
			{
				overlayView = new ZxingOverlayView(UIScreen.MainScreen.Bounds);
				overlayView.BackgroundColor = UIColor.Clear;
				//overlayView.Alpha = 0.4f;
				this.View.AddSubview(overlayView);
			}

			if (ShowButtons)
			{
				buttonCancel = new UIButton(UIButtonType.RoundedRect);
				buttonCancel.Frame = new System.Drawing.RectangleF(20, 20, 130, 30);
				buttonCancel.SetTitle(resxMgr.GetString("Cancel"), UIControlState.Normal);
				buttonCancel.Alpha = 0.3f;
				buttonCancel.SetTitleColor(UIColor.White, UIControlState.Normal);
				buttonCancel.TintColor = UIColor.Gray;
				buttonCancel.TouchUpInside += (sender, e) => {
					this.Scan(null);
				};

				this.View.AddSubview(buttonCancel);

				buttonFlash = new UIButton(UIButtonType.RoundedRect);
				buttonFlash.Frame = new System.Drawing.RectangleF(170, 20, 130, 30);
				buttonFlash.SetTitle(resxMgr.GetString("FlashOn"), UIControlState.Normal);
				buttonFlash.Alpha = 0.3f;
				buttonFlash.TintColor = UIColor.Gray;
				buttonFlash.SetTitleColor(UIColor.White, UIControlState.Normal);
				//buttonFlash.HorizontalAlignment = UIControlContentHorizontalAlignment.Right;
				buttonFlash.TouchUpInside += (sender, e) => {
					if (captureDevice != null)
					{
						if (captureDevice.TorchAvailable)
						{
							MonoTouch.Foundation.NSError err = null;

							captureDevice.LockForConfiguration(out err);

							if (captureDevice.TorchMode == AVCaptureTorchMode.Auto || captureDevice.TorchMode == AVCaptureTorchMode.Off)
								captureDevice.TorchMode = AVCaptureTorchMode.On;
							else
								captureDevice.TorchMode = AVCaptureTorchMode.Off;

							captureDevice.UnlockForConfiguration();

							this.BeginInvokeOnMainThread(() => {
								if (buttonFlash.CurrentTitle == resxMgr.GetString("FlashOn"))
									buttonFlash.SetTitle(resxMgr.GetString("FlashOff"), UIControlState.Normal);
								else
									buttonFlash.SetTitle(resxMgr.GetString("FlashOn"), UIControlState.Normal);
							});
						}
					}
				};
				this.View.AddSubview(buttonFlash);
			}
		}
		bool SetupCaptureSession ()
		{
			// configure the capture session for low resolution, change this if your code
			// can cope with more data or volume
			session = new AVCaptureSession () {
				SessionPreset = AVCaptureSession.PresetMedium
			};

			// create a device input and attach it to the session
			captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType (AVMediaType.Video);
			var input = AVCaptureDeviceInput.FromDevice (captureDevice);
			if (input == null){
				// No input device
				return false;
			}
			session.AddInput (input);

			// create a VideoDataOutput and add it to the sesion
			var output = new AVCaptureVideoDataOutput () {
				VideoSettings = new AVVideoSettings (CVPixelFormatType.CV32BGRA)
			};


			// configure the output
			queue = new DispatchQueue ("myQueue");
			scanner = new ZxingScanner (this);
			output.SetSampleBufferDelegateAndQueue (scanner, queue);
			session.AddOutput (output);
		
			previewLayer = new AVCaptureVideoPreviewLayer (session);
			previewLayer.Orientation = AVCaptureVideoOrientation.Portrait;
			previewLayer.VideoGravity = "AVLayerVideoGravityResizeAspectFill";

			session.StartRunning ();
			return true;
		}
	
		class ZxingScanner : AVCaptureVideoDataOutputSampleBufferDelegate
		{
			bool gotResult = false;
			ZxingViewController parent;
			MultiFormatReader reader;
			byte [] bytes;

			public ZxingScanner (ZxingViewController parent)
			{
				this.parent = parent;

				this.reader = this.parent.Options.BuildMultiFormatReader();
			}

			public override void DidOutputSampleBuffer (AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				try {

					if (!gotResult)
					{
						LuminanceSource luminance;
						//connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;

						using (var pixelBuffer = sampleBuffer.GetImageBuffer () as CVPixelBuffer) {
		
							if (bytes == null)
								bytes = new byte [pixelBuffer.Height * pixelBuffer.BytesPerRow];
		
							pixelBuffer.Lock (0);
							Marshal.Copy (pixelBuffer.BaseAddress, bytes, 0, bytes.Length);
		
							luminance = new RGBLuminanceSource (bytes, pixelBuffer.Width, pixelBuffer.Height);


							pixelBuffer.Unlock (0);
						}

						var binarized = new BinaryBitmap (new HybridBinarizer (luminance));
						var result = reader.decodeWithState (binarized);

						//parent.session.StopRunning ();

						gotResult = true;

					
						if (parent.Scan != null)
							parent.Scan (result);
					}

				} catch (ReaderException) {

					// ignore this exception; it happens every time there is a failed scan

				} catch (Exception e) {

					// TODO: this one is unexpected.. log or otherwise handle it

					throw;

				} finally {
					try {
						// lamest thing, but seems that this throws :(
						sampleBuffer.Dispose ();
					} catch { }
				}
			}
		}
	}
}