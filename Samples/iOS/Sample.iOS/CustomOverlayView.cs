using System;

#if __UNIFIED__
using Foundation;
using CoreFoundation;
using CoreGraphics;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;

using System.Drawing;
using CGRect = global::System.Drawing.RectangleF;
using CGPoint = global::System.Drawing.PointF;
#endif

namespace Sample.iOS
{
	public class CustomOverlayView : UIView
	{
		public UIButton ButtonTorch;
		public UIButton ButtonCancel;

		public CustomOverlayView () : base()
		{
			ButtonTorch = UIButton.FromType(UIButtonType.RoundedRect);
			ButtonTorch.Frame = new CGRect(this.Frame.Width / 2 + 5, this.Frame.Height - 60, this.Frame.Width / 2 - 10, 34);
			ButtonTorch.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin;
			ButtonTorch.SetTitle("Torch", UIControlState.Normal);
			this.AddSubview(ButtonTorch);

		
			ButtonCancel = UIButton.FromType(UIButtonType.RoundedRect);
			ButtonCancel.Frame = new CGRect(0, this.Frame.Height - 60, this.Frame.Width / 2 - 10, 34);
			ButtonCancel.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleRightMargin;
			ButtonCancel.SetTitle("Cancel", UIControlState.Normal);
			this.AddSubview(ButtonCancel);

		}

		public override void LayoutSubviews ()
		{
			ButtonTorch.Frame = new CGRect(this.Frame.Width / 2 + 5, this.Frame.Height - 60, this.Frame.Width / 2 - 10, 34);
			ButtonCancel.Frame = new CGRect(0, this.Frame.Height - 60, this.Frame.Width / 2 - 10, 34);
		}

	}
}

