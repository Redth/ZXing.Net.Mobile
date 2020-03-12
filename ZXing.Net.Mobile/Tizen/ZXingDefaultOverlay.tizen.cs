using ElmSharp;

namespace ZXing.Mobile
{
	class ZXingDefaultOverlay : Box
	{
		Label topLabel;
		Label bottomLabel;

		public ZXingDefaultOverlay(EvasObject parent) : base(parent)
		{
			AlignmentX = -1;
			AlignmentY = -1;
			WeightX = 1;
			WeightY = 1;
		
			InitView();
			
			BackgroundColor = Color.Transparent;
		}

		void InitView()
		{
			topLabel = new Label(this)
			{
				Text = "",
				TextStyle = "DEFAULT = 'font_size=30 align=center valign=bottom'",
				AlignmentX = -1.0,
				AlignmentY = -1.0,
				WeightX = 1.0,
				WeightY = 0.2,
				BackgroundColor = Color.FromRgba(256, 256, 256, 200),
			};
			topLabel.Show();
			PackEnd(topLabel);

			var blankAbove = new ElmSharp.Rectangle(this)
			{
				AlignmentX = -1.0,
				AlignmentY = -1.0,
				WeightX = 1.0,
				WeightY = 0.3,
				Color = Color.Transparent,
			};
			blankAbove.Show();
			PackEnd(blankAbove);

			var middleBar = new ElmSharp.Rectangle(this)
			{
				AlignmentX = -1.0,
				AlignmentY = -1.0,
				WeightX = 1.0,
				WeightY = 0.001,
				Color = Color.Red,
			};
			middleBar.Show();
			PackEnd(middleBar);

			var blankBelow = new ElmSharp.Rectangle(this)
			{
				AlignmentX = -1.0,
				AlignmentY = -1.0,
				WeightX = 1.0,
				WeightY = 0.3,
				Color = Color.Transparent,
			};
			blankBelow.Show();
			PackEnd(blankBelow);

			bottomLabel = new Label(this)
			{
				Text = "",
				TextStyle = "DEFAULT = 'font_size=30 align=center valign=bottom'",
				AlignmentX = -1.0,
				AlignmentY = -1.0,
				WeightX = 1.0,
				WeightY = 0.2,
				BackgroundColor = Color.FromRgba(256, 256, 256, 200),
			};

			bottomLabel.Show();
			PackEnd(bottomLabel);
		}

		public void SetText(string top, string bottom)
		{
			topLabel.Text = top;
			bottomLabel.Text = bottom;
		}
	}
}
