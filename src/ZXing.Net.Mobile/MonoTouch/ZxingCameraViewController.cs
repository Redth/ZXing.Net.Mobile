using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

using MonoTouch.UIKit;
using MonoTouch.Foundation;
using ZXing;

namespace ZXing.Mobile
{
    
	public class BarCodeEventArgs : EventArgs
    {
		
		public BarCodeEventArgs(Result result)
		{
		   	BarcodeResult = result;
		}
		
		public Result BarcodeResult  {
			get;
			set;
		}
		
	
		
    }

	// Delegate declaration.
    public delegate void BarCodeEventHandler(BarCodeEventArgs e);
	
	
	public class ZxingCameraViewController : UIImagePickerController
    {
        public ZxingSurfaceView SurfaceView;
       	public event BarCodeEventHandler BarCodeEvent;

		public MobileBarcodeScanningOptions ScanningOptions { get;set; }
		public MobileBarcodeScanner Scanner { get;set; }

        public ZxingCameraViewController(MobileBarcodeScanningOptions options, MobileBarcodeScanner scanner)
            : base()
        {
			this.ScanningOptions = options;
			this.Scanner = scanner;
            Initialize();
        }
       	
		#region Public methods
		public void Initialize()
        {
			this.InvokeOnMainThread (() => {
				if (IsSourceTypeAvailable (UIImagePickerControllerSourceType.Camera))
					SourceType = UIImagePickerControllerSourceType.Camera;
				else
					SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
			

			
				ShowsCameraControls = false;
				AllowsEditing = true;
				WantsFullScreenLayout = true;
				var tf = MonoTouch.CoreGraphics.CGAffineTransform.MakeScale (1.25f, 1.25f); //CGAffineTransformScale(_picker.cameraViewTransform, CAMERA_TRANSFORM, CAMERA_TRANSFORM);
				CameraViewTransform = tf;

				UIView overlayView = null;

				if (Scanner.UseCustomOverlay && Scanner.CustomOverlay != null)
					overlayView = Scanner.CustomOverlay;

				SurfaceView = new ZxingSurfaceView (this, Scanner, this.ScanningOptions, overlayView);

				CameraOverlayView = SurfaceView;
			});
        }
       	
		
        public void BarCodeScanned(Result result)
        {
          	if (SurfaceView != null)
                SurfaceView.StopWorker();
			
			if(_imageView!=null)
			{
				_imageView.RemoveFromSuperview();
			}
			
			//if(result!=null)
			//{
					BarCodeEventArgs eventArgs = new BarCodeEventArgs(result);
					BarCodeEventHandler handler  = BarCodeEvent;
					if (handler != null)
					{
						// Invokes the delegates.
						handler(eventArgs);
						BarCodeEvent = null;
					}
				
		
			//}
			
		}
		 #endregion
		
        public virtual void DismissViewController()
        {
			if (torch)
				ToggleTorch();

          	if (SurfaceView != null)
                SurfaceView.StopWorker();
		
			if(_imageView!=null)
			{
				_imageView.RemoveFromSuperview();
			}
			
			DismissModalViewControllerAnimated(true);
			
			
	    }
       
		
		
		
		#region Override methods
        public override void LoadView()
        {
            base.LoadView();

		}

		bool torch = false;

		public bool IsTorchOn { get { return torch; } }

		public void ToggleTorch()
		{
			try
			{
			NSError err;

			var device = MonoTouch.AVFoundation.AVCaptureDevice.DefaultDeviceWithMediaType(MonoTouch.AVFoundation.AVMediaType.Video);
			device.LockForConfiguration(out err);

			if (!torch)
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

			torch = !torch;
			}
			catch { }

		}

		public void Torch(bool on)
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

		private UIImageView _imageView;
		
		public override void ViewWillAppear (bool animated)
		{
			
			base.ViewWillAppear (animated);



			this.SurfaceView.StartWorker();

				/*
				_imageView = new UIImageView(new RectangleF(0, 0, 320, 480 - 54));
				//_imageView.BackgroundColor = UIColor.Gray;
				_imageView.Image = UIImage.FromBundle("Images/BarCodeOverlay.png");
				Add(_imageView);
				_imageView.Image.Dispose();
				
				//The iris - we don't want that one
				NSTimer timer = NSTimer.CreateScheduledTimer(new TimeSpan(0, 0, 2),delegate
				{
				
				
					_imageView.RemoveFromSuperview();
						
					this.OverlayView.StartWorker();
				
			
				});
				*/
			
//				NSNotificationCenter.DefaultCenter.AddObserver(new NSString("PLCameraViewIrisAnimationDidEndNotification"), (notification) => {   
//			        
//					if(this.View != null)
//					{
//						_imageView.RemoveFromSuperview();
//						
//						this.OverlayView.StartWorker();
//					}
//				});   
			
			
		}
		

		
			
	
		
        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
			
			if (SurfaceView != null)
                SurfaceView.StopWorker();
			
        }
        #endregion

       
		
			
    }
}

