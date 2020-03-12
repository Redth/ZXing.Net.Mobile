using System;

using Foundation;
using CoreFoundation;
using CoreGraphics;
using UIKit;

namespace Sample.iOS
{
	public class CustomOverlayView : UIView
	{
		public UIButton ButtonTorch;
		public UIButton ButtonCancel;

		public CustomOverlayView() : base()
		{
			ButtonTorch = UIButton.FromType(UIButtonType.RoundedRect);
			ButtonTorch.Frame = new CGRect(Frame.Width / 2 + 5, Frame.Height - 60, Frame.Width / 2 - 10, 34);
			ButtonTorch.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin;
			ButtonTorch.SetTitle("Torch", UIControlState.Normal);
			AddSubview(ButtonTorch);


			ButtonCancel = UIButton.FromType(UIButtonType.RoundedRect);
			ButtonCancel.Frame = new CGRect(0, Frame.Height - 60, Frame.Width / 2 - 10, 34);
			ButtonCancel.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleRightMargin;
			ButtonCancel.SetTitle("Cancel", UIControlState.Normal);
			AddSubview(ButtonCancel);
		}

		public override void LayoutSubviews()
		{
			ButtonTorch.Frame = new CGRect(Frame.Width / 2 + 5, Frame.Height - 60, Frame.Width / 2 - 10, 34);
			ButtonCancel.Frame = new CGRect(0, Frame.Height - 60, Frame.Width / 2 - 10, 34);
		}

	}
}

