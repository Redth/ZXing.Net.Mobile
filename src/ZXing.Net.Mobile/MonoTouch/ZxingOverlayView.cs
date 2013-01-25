using System;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using MonoTouch.UIKit;
using MonoTouch.CoreFoundation;
using MonoTouch.AVFoundation;
using MonoTouch.CoreVideo;
using MonoTouch.CoreMedia;
using MonoTouch.CoreGraphics;
using ZXing;
using ZXing.Common;

namespace ZXing.Mobile
{
	public class ZxingOverlayView : UIView
	{
		public ZxingOverlayView (RectangleF frame) : base(frame)
		{
	
		}

		Random rnd = new Random();

		float laserAlpha = 0.5f;

		public override void Draw (RectangleF rect)
		{
			var width = rect.Width * 15/16;
			var height = rect.Height * 4/10;

			var xPad = (rect.Width - width) / 2;
			var yPad = (rect.Height - height) / 2;

			var viewFinderRect = new RectangleF(xPad, yPad, width, height);

			var cg = UIGraphics.GetCurrentContext();
			cg.ClearRect(viewFinderRect);

			cg.SetFillColor(0.0f, 0.0f, 0.0f, 0.6f);

			cg.FillRect(new RectangleF(0, 0, xPad, rect.Height));
			cg.FillRect(new RectangleF(xPad + width, 0, xPad, rect.Height));

			cg.FillRect(new RectangleF(xPad, 0, width, yPad));
			cg.FillRect(new RectangleF(xPad, yPad + height, width, yPad));

			cg.SetStrokeColor(1.0f, 0.9f);
			cg.SetLineWidth(3.0f);
			cg.StrokeRect(viewFinderRect);
			cg.ClearRect(viewFinderRect);

			//var alpha = rnd.Next(0.5f, 0.9f);
			laserAlpha += 0.1f;

			if (laserAlpha > 0.9f)
				laserAlpha = 0.5f;



			cg.SetFillColor(1.0f, 0.2f, 0.2f, laserAlpha);
			cg.FillRect(new RectangleF(xPad + 1, yPad + (height / 2) - 2, width - 2, 4));

			var tc=	new System.Threading.TimerCallback((o) => {
				
				this.BeginInvokeOnMainThread(() => this.SetNeedsDisplay());

			});
			var t = new System.Threading.Timer(tc, null, 250, System.Threading.Timeout.Infinite);
		
		}
	}
}

