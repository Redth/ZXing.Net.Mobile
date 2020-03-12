using System;
using System.Collections.Generic;

using Foundation;
using CoreGraphics;
using CoreFoundation;
using UIKit;
using AVFoundation;

using ZXing.Mobile;

namespace ZXing.Mobile
{
	public class ZXingDefaultOverlayView : UIView
	{
		public ZXingDefaultOverlayView(CGRect frame, string topText,
										string bottomText, string cancelText, string flashText,
										Action onCancel, Action onTorch) : base(frame)
		{
			this.cancelText = cancelText ?? "Cancel";
			this.flashText = flashText ?? "Flash";
			this.topText = topText ?? "";
			this.bottomText = bottomText ?? "";

			this.onCancel = onCancel;
			this.onTorch = onTorch;
			Initialize();
		}

		readonly string cancelText;
		readonly string flashText;
		readonly string topText;
		readonly string bottomText;

		readonly Action onCancel;
		readonly Action onTorch;

		UIView topBg;
		UIView bottomBg;
		UILabel textTop;
		UILabel textBottom;
		UIView redLine;

		void Initialize()
		{
			Opaque = false;
			BackgroundColor = UIColor.Clear;

			// Add(_mainView);
			var picFrameWidth = Math.Round(Frame.Width * 0.90); // screenFrame.Width;
			var picFrameHeight = Math.Round(Frame.Height * 0.60);
			var picFrameX = (Frame.Width - picFrameWidth) / 2;
			var picFrameY = (Frame.Height - picFrameHeight) / 2;

			var picFrame = new CGRect((int)picFrameX, (int)picFrameY, (int)picFrameWidth, (int)picFrameHeight);

			//Setup Overlay
			var overlaySize = new CGSize(Frame.Width, Frame.Height - 44);

			topBg = new UIView(new CGRect(0, 0, overlaySize.Width, (overlaySize.Height - picFrame.Height) / 2));
			topBg.Frame = new CGRect(0, 0, overlaySize.Width, overlaySize.Height * 0.30f);
			topBg.BackgroundColor = UIColor.Black;
			topBg.Alpha = 0.6f;
			topBg.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleBottomMargin;

			bottomBg = new UIView(new CGRect(0, topBg.Frame.Height + picFrame.Height, overlaySize.Width, topBg.Frame.Height));
			bottomBg.Frame = new CGRect(0, overlaySize.Height * 0.70f, overlaySize.Width, overlaySize.Height * 0.30f);
			bottomBg.BackgroundColor = UIColor.Black;
			bottomBg.Alpha = 0.6f;
			bottomBg.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin;

			redLine = new UIView(new CGRect(0, overlaySize.Height * 0.5f - 2.0f, overlaySize.Width, 4.0f));
			redLine.BackgroundColor = UIColor.Red;
			redLine.Alpha = 0.4f;
			redLine.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleBottomMargin | UIViewAutoresizing.FlexibleTopMargin;

			AddSubview(redLine);
			AddSubview(topBg);
			AddSubview(bottomBg);

			var topTextLines = 1;

			if (!string.IsNullOrEmpty(topText))
				topTextLines = topText.Split('\n').Length;

			var botTextLines = 1;

			if (!string.IsNullOrEmpty(bottomText))
				botTextLines = bottomText.Split('\n').Length;


			textTop = new UILabel()
			{
				Frame = topBg.Frame,
				Text = topText,
				Font = UIFont.SystemFontOfSize(13),
				TextAlignment = UITextAlignment.Center,
				TextColor = UIColor.White,
				Lines = 0,
				BackgroundColor = UIColor.Clear
			};

			textTop.SizeToFit();
			AddSubview(textTop);

			textBottom = new UILabel()
			{
				Frame = bottomBg.Frame,
				Text = bottomText,
				Font = UIFont.SystemFontOfSize(13),
				TextAlignment = UITextAlignment.Center,
				TextColor = UIColor.White,
				Lines = 0,
				BackgroundColor = UIColor.Clear
			};

			textBottom.SizeToFit();
			AddSubview(textBottom);

			var captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);

			var hasTorch = false;

			if (captureDevice != null)
				hasTorch = captureDevice.TorchAvailable;

			InvokeOnMainThread(delegate
			{
				// Setting tool bar
				var toolBar = new UIToolbar(new CGRect(0, Frame.Height - 44, Frame.Width, 44));

				var buttons = new List<UIBarButtonItem>();
				buttons.Add(new UIBarButtonItem(cancelText, UIBarButtonItemStyle.Done,
												delegate { onCancel(); }));

				if (hasTorch)
				{
					buttons.Add(new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace));
					buttons.Add(new UIBarButtonItem(flashText, UIBarButtonItemStyle.Done,
													delegate { onTorch(); }));
				}

				toolBar.Items = buttons.ToArray();

				toolBar.TintColor = UIColor.Black;
				toolBar.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin;
				Add(toolBar);
			});
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			var overlaySize = new CGSize(Frame.Width, Frame.Height - 44);

			if (topBg != null)
				topBg.Frame = new CGRect(0, 0, overlaySize.Width, overlaySize.Height * 0.30f);
			if (bottomBg != null)
				bottomBg.Frame = new CGRect(0, overlaySize.Height * 0.70f, overlaySize.Width, overlaySize.Height * 0.30f);

			if (textTop != null)
				textTop.Frame = topBg.Frame;//  new RectangleF(0, overlaySize.Height *  0.10f, overlaySize.Width, 42);
			if (textBottom != null)
				textBottom.Frame = bottomBg.Frame; // new RectangleF(0, overlaySize.Height *  0.825f - 32f, overlaySize.Width, 64);

			if (redLine != null)
				redLine.Frame = new CGRect(0, overlaySize.Height * 0.5f - 2.0f, overlaySize.Width, 4.0f);
		}

		public void Destroy()
			=> InvokeOnMainThread(() =>
			{
				textTop.RemoveFromSuperview();
				textBottom.RemoveFromSuperview();
				topBg.RemoveFromSuperview();
				bottomBg.RemoveFromSuperview();
				redLine.RemoveFromSuperview();

				textBottom = null;
				textTop = null;
				topBg = null;
				bottomBg = null;
				redLine = null;
			});
	}
}

