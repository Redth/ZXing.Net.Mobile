using System;
using System.Drawing;
using System.Collections.Generic;
using System.Resources;

using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using ZXing;
using ZXing.Common;
using System.Collections;
using MonoTouch.AudioToolbox;
//using BarcodeTesting.Utilities;
using MonoTouch.AVFoundation;

namespace ZXing.Mobile
{
    
	/*public class ResultCallBack : ResultPointCallback
	{
		ZxingSurfaceView _view;
		public ResultCallBack(ZxingSurfaceView view)
		{
			_view = view;
		}
		
		
		#region ResultPointCallback implementation
		public void foundPossibleResultPoint (ResultPoint point)
		{
			if(point!=null)
			{
				//_view.SetArrows(true, true);
			} else {
				//_view.SetArrows(false, true);
			}
		}
		#endregion	
		
	}*/
	
	public class ZxingSurfaceView : UIView
    {
        #region Variables
        public NSTimer WorkerTimer;
		
     	private ZxingCameraViewController _parentViewController;
		
		//private UIImageView _mainView;
		//private UIImageView _greenTopArrow;
		//private UIImageView _greenBottomArrow;
		//private UIImageView _whiteTopArrow;
		//private UIImageView _whiteBottomArrow;
		//private UILabel _textCue;
		//private UIImageView _otherView1, _otherView2, _focusView1, _focusView2;
		//private bool _isGreen;
		private Hashtable hints;
		
		//private static com.google.zxing.oned.MultiFormatOneDReader _multiFormatReader = null;
		private static MultiFormatReader _multiFormatReader = null;

		//private static RectangleF picFrame = new RectangleF(0, 146, 320, 157);
		private static RectangleF picFrame = new RectangleF(); //, UIScreen.MainScreen.Bounds.Width, 257);

		//private static UIImage _theScreenImage = null;

	  #endregion

		/// <summary>
		/// Gets a value indicating whether this iOS device has flash.
		/// </summary>
		/// <value><c>true</c> if this iOS device has flash; otherwise, <c>false</c>.</value>
		private bool HasFlash
		{
			get
			{
				var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
				return device.FlashAvailable;
			}
		}

		public UIView OverlayView { get;set; }
		public MobileBarcodeScanner Scanner { get;set; }
		public MobileBarcodeScanningOptions Options { get;set; }
		
        public ZxingSurfaceView(ZxingCameraViewController parentController, MobileBarcodeScanner scanner, MobileBarcodeScanningOptions options, UIView overlayView) : base()
        {
			var screenFrame = UIScreen.MainScreen.Bounds;
			var scale = UIScreen.MainScreen.Scale;
			screenFrame = new RectangleF(screenFrame.X, screenFrame.Y, screenFrame.Width * scale, screenFrame.Height * scale);

			var picFrameWidth = Math.Round(screenFrame.Width * 0.90); //screenFrame.Width;
			var picFrameHeight = Math.Round(screenFrame.Height * 0.60);
			var picFrameX = (screenFrame.Width - picFrameWidth) / 2;
			var picFrameY = (screenFrame.Height - picFrameHeight) / 2;

			picFrame = new RectangleF((int)picFrameX, (int)picFrameY, (int)picFrameWidth, (int)picFrameHeight);

			//Console.WriteLine(picFrame.X + ", " + picFrame.Y + " -> " + picFrame.Width + " x " + picFrame.Height);
			this.OverlayView = overlayView;
			this.Scanner = scanner;
			this.Options = options;
			
            Initialize();
			_parentViewController = parentController;
        }
		
		#region Private methods
		private void Initialize ()
		{            
           	
			Frame = new RectangleF (0, 0, (UIScreen.MainScreen.Bounds.Width), (UIScreen.MainScreen.Bounds.Height));
			Opaque = false;
			BackgroundColor = UIColor.Clear;

			//Add(_mainView);
				

			if (this.OverlayView != null) 
			{
				this.OverlayView.Frame = Frame;
				this.AddSubview(this.OverlayView);
			}
			else
			{
				//Setup Overlay
				var overlaySize = new SizeF (this.Frame.Width, this.Frame.Height - 44);

				var topBg = new UIView (new RectangleF (0, 0, this.Frame.Width, (overlaySize.Height - picFrame.Height) / 2));
				topBg.Frame = new RectangleF (0, 0, this.Frame.Width, this.Frame.Height * 0.30f);
				topBg.BackgroundColor = UIColor.Black;
				topBg.Alpha = 0.6f;

				var bottomBg = new UIView (new RectangleF (0, topBg.Frame.Height + picFrame.Height, this.Frame.Width, topBg.Frame.Height));
				bottomBg.Frame = new RectangleF (0, this.Frame.Height * 0.70f, this.Frame.Width, this.Frame.Height * 0.30f);
				bottomBg.BackgroundColor = UIColor.Black;
				bottomBg.Alpha = 0.6f;

				//var grad = new MonoTouch.CoreAnimation.CAGradientLayer();
				//grad.Frame = bottomBg.Bounds;
				//grad.Colors = new CGColor[] { new CGColor()UIColor.Black, UIColor.FromWhiteAlpha(0.0f, 0.6f) };
				//bottomBg.Layer.InsertSublayer(grad, 0);
			
				//			[v.layer insertSublayer:gradient atIndex:0];


				var redLine = new UIView (new RectangleF (0, this.Frame.Height * 0.5f - 2.0f, this.Frame.Width, 4.0f));
				redLine.BackgroundColor = UIColor.Red;
				redLine.Alpha = 0.4f;

				this.AddSubview (redLine);
				this.AddSubview (topBg);
				this.AddSubview (bottomBg);

				var textTop = new UILabel () 
			{
				Frame = new RectangleF(0, this.Frame.Height *  0.10f, this.Frame.Width, 42),
				Text = Scanner.TopText,
				Font = UIFont.SystemFontOfSize(13),
				TextAlignment = UITextAlignment.Center,
				TextColor = UIColor.White,
				Lines = 2,
				BackgroundColor = UIColor.Clear
			};

				this.AddSubview (textTop);

				var textBottom = new UILabel () 
			{
				Frame = new RectangleF(0, this.Frame.Height *  0.825f - 32f, this.Frame.Width, 64),
				Text = Scanner.BottomText,
				Font = UIFont.SystemFontOfSize(13),
				TextAlignment = UITextAlignment.Center,
				TextColor = UIColor.White,
				Lines = 3,
				BackgroundColor = UIColor.Clear

			};

				this.AddSubview (textBottom);


				InvokeOnMainThread(delegate {
					// Setting tool bar
					var toolBar = new UIToolbar(new RectangleF(0, UIScreen.MainScreen.Bounds.Height - 44, UIScreen.MainScreen.Bounds.Width, 44));

					List<UIBarButtonItem> buttons = new List<UIBarButtonItem>();
					buttons.Add(new UIBarButtonItem(Scanner.CancelButtonText, UIBarButtonItemStyle.Done, 
					                                delegate {
						
						_parentViewController.BarCodeScanned(null);
						//DismissViewController();
						
					}));

					if (HasFlash)
					{
						buttons.Add(new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace));
						buttons.Add(new UIBarButtonItem(Scanner.FlashButtonText, UIBarButtonItemStyle.Done,
						                                delegate {
							

							_parentViewController.ToggleTorch();
						}));
					}

					toolBar.Items = buttons.ToArray();

					toolBar.TintColor = UIColor.Black;
					Add(toolBar);
				});	

				//_textCue = new UILabel ()
				//{
				//	Frame = new RectangleF(0,topBg.Frame.Bottom - 50,this.Frame.Width,21),
				//	Text = "Adjust barcode according to arrows",
				//	Font = UIFont.SystemFontOfSize(13),
				//	TextAlignment = UITextAlignment.Center,
				//	TextColor = UIColor.Clear,
				//	Opaque = true,
				//BackgroundColor = UIColor.Clear// UIColor.FromRGBA(0,0,0,0),
				//};
				
				//_greenBottomArrow = new UIImageView (UIImage.FromBundle ("Images/green_down_arrow.png"));
				//_greenBottomArrow.Frame = new RectangleF (0, topBg.Frame.Bottom - 19, this.Frame.Width, 19);
				//_greenTopArrow = new UIImageView (UIImage.FromBundle ("Images/green_up_arrow.png"));
				//_greenTopArrow.Frame = new RectangleF (0, bottomBg.Frame.Top, 320, 19);
				
				//_whiteBottomArrow = new UIImageView (UIImage.FromBundle ("Images/white_down_arrow.png"));
				//_whiteBottomArrow.Frame = new RectangleF (0, topBg.Frame.Bottom - 19, 320, 19);
				//_whiteTopArrow = new UIImageView (UIImage.FromBundle ("Images/white_up_arrow.png"));
				//_whiteTopArrow.Frame = new RectangleF (0, bottomBg.Frame.Top, 320, 19);
				
				
				//AddSubview (_textCue);
				//AddSubview (_greenBottomArrow);
				//AddSubview (_greenTopArrow);
				//AddSubview (_whiteBottomArrow);
				//AddSubview (_whiteTopArrow);
				
				//_greenBottomArrow.Image.Dispose ();
				//_greenTopArrow.Image.Dispose ();
				//_textCue.Dispose ();
				//_whiteBottomArrow.Image.Dispose ();
				//_whiteTopArrow.Image.Dispose ();
				//_mainView.Image.Dispose ();
				
			}
				// initalise flags
				//_isGreen = false;
				//SetArrows(false, false);
			
			UIApplication.SharedApplication.SetStatusBarHidden(false, false);
			UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackOpaque, false);

        
		}

		private void Worker()
        {
       
				
				if(_multiFormatReader == null)
				{
				_multiFormatReader = this.Options.BuildMultiFormatReader();
				//_multiFormatReader = new MultiFormatReader {
				//		Hints = new Hashtable {
				//			{ DecodeHintType.POSSIBLE_FORMATS, new ArrayList { 
				//					BarcodeFormat.UPC_A, BarcodeFormat.UPC_E , BarcodeFormat.CODE_128,
				//					BarcodeFormat.CODE_39, BarcodeFormat.EAN_13, BarcodeFormat.EAN_8
				//				} }
				//		}
				//	};
				}
				
				
			using (var ap = new NSAutoreleasePool())
			{
				// Capturing screen image            
				using (var screenImage = CGImage.ScreenImage.WithImageInRect(picFrame)) //.WithImageInRect(picFrame))
	            {
					using (var _theScreenImage = UIImage.FromImage(screenImage))
					using (var srcbitmap = new Bitmap(_theScreenImage))
					{
						LuminanceSource source = null;
						BinaryBitmap bitmap = null;
						try 
						{
							//Console.WriteLine(screenImage.Width.ToString() + " x " + screenImage.Height.ToString());

							//var cropY = (int)((screenImage.Height * 0.4) / 2);
							source = new RGBLuminanceSource(srcbitmap, screenImage.Width, screenImage.Height); //.crop(0, cropY, 0, screenImage.Height - cropY - cropY);

							//Console.WriteLine(source.Width + " x " + source.Height);

			              	bitmap = new BinaryBitmap(new HybridBinarizer(source));
												
					
						try
						{
							var result = _multiFormatReader.decodeWithState(bitmap); //
							//var result = _multiFormatReader.decodeWithState (bitmap);
						
								//srcbitmap.Dispose();

							if(result != null && result.Text!=null)
							{
								//BeepOrVibrate();
								_parentViewController.BarCodeScanned(result);
							}
						}
						catch (ReaderException)
						{
						}

						
					/*
						com.google.zxing.common.BitArray row = new com.google.zxing.common.BitArray(screenImage.Width);
						
						int middle = screenImage.Height >> 1;
						int rowStep = System.Math.Max(1, screenImage.Height >> (4));
						
						for (int x = 0; x < 9; x++)
						{
							
							// Scanning from the middle out. Determine which row we're looking at next:
							int rowStepsAboveOrBelow = (x + 1) >> 1;
							bool isAbove = (x & 0x01) == 0; // i.e. is x even?
							int rowNumber = middle + rowStep * (isAbove?rowStepsAboveOrBelow:- rowStepsAboveOrBelow);
							if (rowNumber < 0 || rowNumber >= screenImage.Height)
							{
								// Oops, if we run off the top or bottom, stop
								break;
							}
							
							// Estimate black point for this row and load it:
							try
							{
								row = bitmap.getBlackRow(rowNumber, row);
								
								
								var resultb = _multiFormatReader.decodeRow(rowNumber, row, hints);
								if(resultb.Text!=null)
								{
									Console.WriteLine("SCANNED");
									BeepOrVibrate();
									_parentViewController.BarCodeScanned(resultb);
										
								
									break;
								}
								else {
									continue;
								}
								
							}
							catch (ReaderException re)
							{
								continue;
							}
					
						}
*/
						
						
	//					var result = _barcodeReader.decodeWithState(bitmap);
	//					
	//					if(result.Text!=null)
	//					{
	//						_multiFormatOneDReader = null;
	//						BeepOrVibrate();
	//						_parentViewController.BarCodeScanned(result);
	//					}
						
					} catch (Exception ex) {
						Console.WriteLine(ex.Message);
					}
					finally {
						if(bitmap!=null)
							bitmap = null;

						 if(source!=null)
							source = null;
						
		              //  if(srcbitmap!=null)
						//	srcbitmap = null;
					
						//if (_theScreenImage != null)
						//	_theScreenImage = null;

						
					}	
					}
				}
	      
	        }

			GC.Collect();

			//Console.WriteLine("Done.");

        }
		
		//private void BeepOrVibrate()
		//{
		//	SystemSound.FromFile("Sounds/beep.wav").PlayAlertSound();
		//}
		#endregion

        #region Public methods
		/*public void SetArrows(bool inRange, bool animated)
		{
			
			
			// already showing green bars
			if (inRange && _isGreen) return;
			
			// update flag
			_isGreen = inRange;
			
			if (_isGreen)
			{
				_textCue.Text =  "Hold still for scanning";
				_otherView1 = _whiteTopArrow; 
				_otherView2 = _whiteBottomArrow;
				_focusView1 = _greenTopArrow; 
				_focusView2 = _greenBottomArrow;
			}
			else
			{
				_textCue.Text =  "Adjust barcode according to arrows";
				_focusView1 = _whiteTopArrow; 
				_focusView2 = _whiteBottomArrow;
				_otherView1 = _greenTopArrow; 
				_otherView2 = _greenBottomArrow;
			}
			
			if (animated)
			{
				UIView.BeginAnimations("");
				UIView.SetAnimationDuration(0.15f);
				UIView.SetAnimationBeginsFromCurrentState(true);
			}
			
			_focusView1.Alpha = 1;
			_focusView2.Alpha = 1;
			
			_otherView1.Alpha = 0;
			_otherView2.Alpha = 0;
			
			if (animated) UIView.CommitAnimations();
		}*/

		bool firstTimeWorker = true;
		
		public void StartWorker()
        {
           
			if(WorkerTimer!=null)
			{
				return;
			}

			var delay = TimeSpan.FromMilliseconds(_parentViewController.ScanningOptions.DelayBetweenAnalyzingFrames);

			if (firstTimeWorker)
			{
				delay = TimeSpan.FromMilliseconds(_parentViewController.ScanningOptions.InitialDelayBeforeAnalyzingFrames);
				firstTimeWorker = false;
			}
			
			 WorkerTimer = NSTimer.CreateRepeatingTimer(delay, delegate { 
				Worker(); 
			});
			NSRunLoop.Current.AddTimer(WorkerTimer, NSRunLoopMode.Default);
			
			
            
        }
        
		public void StopWorker()
        {
            // starting timer
            if (WorkerTimer != null)
            {
                WorkerTimer.Invalidate();
				try { WorkerTimer.Dispose(); } catch { }
                WorkerTimer = null;
				try { NSRunLoop.Current.Dispose(); } catch { }
            }
			
			//Just in case
			_multiFormatReader = null;
			hints = null;
        }
        #endregion
		
		
        #region Override methods       
        protected override void Dispose(bool disposing)
        {
            StopWorker();
			//try
			//{
			//_greenBottomArrow.Dispose();
			//_greenTopArrow.Dispose();
			//_textCue.Dispose();
			//_whiteBottomArrow.Dispose();
			//_whiteTopArrow.Dispose();
			//_mainView.Dispose();
			 //_otherView1.Dispose();
			//_//otherView2.Dispose();
			//_focusView1.Dispose();
			//_focusView2.Dispose();
			//} catch { }
			
            base.Dispose(disposing);
        }
        #endregion
		
    }
}
