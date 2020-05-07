
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Text;

namespace ZXing.Mobile
{
	public class ZxingOverlayView : View
	{
		int[] SCANNER_ALPHA = { 0, 64, 128, 192, 255, 192, 128, 64 };
		const long ANIMATION_DELAY = 80L;
		const int CURRENT_POINT_OPACITY = 0xA0;
		const int MAX_RESULT_POINTS = 20;
		const int POINT_SIZE = 6;

		Paint paint;
		Bitmap resultBitmap;
		Color maskColor;
		Color resultColor;
		Color frameColor;
		Color laserColor;

		int scannerAlpha;
		List<ZXing.ResultPoint> possibleResultPoints;

		public ZxingOverlayView(Context context) : base(context)
		{
			// Initialize these once for performance rather than calling them every time in onDraw().
			paint = new Paint(PaintFlags.AntiAlias);

			//Resources resources = getResources();
			maskColor = Color.Gray; // resources.getColor(R.color.viewfinder_mask);
			resultColor = Color.Red; // resources.getColor(R.color.result_view);
			frameColor = Color.Black; // resources.getColor(R.color.viewfinder_frame);
			laserColor = Color.Red; //  resources.getColor(R.color.viewfinder_laser);
									//resultPointColor = Color.LightCoral; // resources.getColor(R.color.possible_result_points);
			scannerAlpha = 0;
			possibleResultPoints = new List<ZXing.ResultPoint>(5);

			SetBackgroundColor(Color.Transparent);
		}

		Rect GetFramingRect(Canvas canvas)
		{
			var width = canvas.Width * 15 / 16;

			var height = canvas.Height * 4 / 10;

			var leftOffset = (canvas.Width - width) / 2;
			var topOffset = (canvas.Height - height) / 2;
			var framingRect = new Rect(leftOffset, topOffset, leftOffset + width, topOffset + height);

			return framingRect;
		}

		public string TopText { get; set; }
		public string BottomText { get; set; }

		protected override void OnDraw(Canvas canvas)
		{

			var scale = this.Context.Resources.DisplayMetrics.Density;

			var frame = GetFramingRect(canvas);
			if (frame == null)
				return;

			var width = canvas.Width;
			var height = canvas.Height;

			paint.Color = resultBitmap != null ? resultColor : maskColor;
			paint.Alpha = 100;

			canvas.DrawRect(0, 0, width, frame.Top, paint);
			//canvas.DrawRect(0, frame.Top, frame.Left, frame.Bottom + 1, paint);
			//canvas.DrawRect(frame.Right + 1, frame.Top, width, frame.Bottom + 1, paint);
			canvas.DrawRect(0, frame.Bottom + 1, width, height, paint);


			var textPaint = new TextPaint();
			textPaint.Color = Color.White;
			textPaint.TextSize = 16 * scale;

			var topTextLayout = new StaticLayout(this.TopText, textPaint, canvas.Width, Android.Text.Layout.Alignment.AlignCenter, 1.0f, 0.0f, false);
			canvas.Save();
			var topBounds = new Rect();

			textPaint.GetTextBounds(this.TopText, 0, this.TopText.Length, topBounds);
			canvas.Translate(0, frame.Top / 2 - (topTextLayout.Height / 2));

			//canvas.Translate(topBounds.Left, topBounds.Bottom);
			topTextLayout.Draw(canvas);

			canvas.Restore();


			var botTextLayout = new StaticLayout(this.BottomText, textPaint, canvas.Width, Android.Text.Layout.Alignment.AlignCenter, 1.0f, 0.0f, false);
			canvas.Save();
			var botBounds = new Rect();

			textPaint.GetTextBounds(this.BottomText, 0, this.BottomText.Length, botBounds);
			canvas.Translate(0, (frame.Bottom + (canvas.Height - frame.Bottom) / 2) - (botTextLayout.Height / 2));

			//canvas.Translate(topBounds.Left, topBounds.Bottom);
			botTextLayout.Draw(canvas);

			canvas.Restore();





			if (resultBitmap != null)
			{
				paint.Alpha = CURRENT_POINT_OPACITY;
				canvas.DrawBitmap(resultBitmap, null, new RectF(frame.Left, frame.Top, frame.Right, frame.Bottom), paint);
			}
			else
			{
				// Draw a two pixel solid black border inside the framing rect
				paint.Color = frameColor;
				//canvas.DrawRect(frame.Left, frame.Top, frame.Right + 1, frame.Top + 2, paint);
				//canvas.DrawRect(frame.Left, frame.Top + 2, frame.Left + 2, frame.Bottom - 1, paint);
				//canvas.DrawRect(frame.Right - 1, frame.Top, frame.Right + 1, frame.Bottom - 1, paint);
				//canvas.DrawRect(frame.Left, frame.Bottom - 1, frame.Right + 1, frame.Bottom + 1, paint);

				// Draw a red "laser scanner" line through the middle to show decoding is active
				paint.Color = laserColor;
				paint.Alpha = SCANNER_ALPHA[scannerAlpha];
				scannerAlpha = (scannerAlpha + 1) % SCANNER_ALPHA.Length;
				var middle = frame.Height() / 2 + frame.Top;
				//int middle = frame.Width() / 2 + frame.Left;

				//canvas.DrawRect(frame.Left + 2, middle - 1, frame.Right - 1, middle + 2, paint);

				canvas.DrawRect(0, middle - 1, width, middle + 2, paint);
				//canvas.DrawRect(middle - 1, frame.Top + 2, middle + 2, frame.Bottom - 1, paint); //frame.Top + 2, middle - 1, frame.Bottom - 1, middle + 2, paint);

				//var previewFrame = scanner.GetFramingRectInPreview();
				//float scaleX = frame.Width() / (float) previewFrame.Width();
				//float scaleY = frame.Height() / (float) previewFrame.Height();

				/*var currentPossible = possibleResultPoints;
				var currentLast = lastPossibleResultPoints;

				int frameLeft = frame.Left;
				int frameTop = frame.Top;

				if (currentPossible == null || currentPossible.Count <= 0) 
				{
					lastPossibleResultPoints = null;
				} 
				else 
				{
					possibleResultPoints = new List<com.google.zxing.ResultPoint>(5);
					lastPossibleResultPoints = currentPossible;
					paint.Alpha = CURRENT_POINT_OPACITY;
					paint.Color = resultPointColor;

					lock (currentPossible) 
					{
						foreach (var point in currentPossible) 
						{
							canvas.DrawCircle(frameLeft + (int) (point.X * scaleX),
							frameTop + (int) (point.Y * scaleY), POINT_SIZE, paint);
						}
					}
				}

				if (currentLast != null) 
				{
					paint.Alpha = CURRENT_POINT_OPACITY / 2;
					paint.Color = resultPointColor;

					lock (currentLast) 
					{
						float radius = POINT_SIZE / 2.0f;
						foreach (var point in currentLast) 
						{
							canvas.DrawCircle(frameLeft + (int) (point.X * scaleX),
							frameTop + (int) (point.Y * scaleY), radius, paint);
						}
					}
				}
				*/

				// Request another update at the animation interval, but only repaint the laser line,
				// not the entire viewfinder mask.
				PostInvalidateDelayed(ANIMATION_DELAY,
									  frame.Left - POINT_SIZE,
									  frame.Top - POINT_SIZE,
									  frame.Right + POINT_SIZE,
									  frame.Bottom + POINT_SIZE);
			}

			base.OnDraw(canvas);
		}

		public void DrawResultBitmap(Android.Graphics.Bitmap barcode)
		{
			resultBitmap = barcode;
			Invalidate();
		}

		public void AddPossibleResultPoint(ZXing.ResultPoint point)
		{
			var points = possibleResultPoints;

			lock (points)
			{
				points.Add(point);
				var size = points.Count;
				if (size > MAX_RESULT_POINTS)
				{
					points.RemoveRange(0, size - MAX_RESULT_POINTS / 2);
				}
			}
		}
	}
}
