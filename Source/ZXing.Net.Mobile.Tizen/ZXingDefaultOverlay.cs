using ElmSharp;

namespace ZXing.Mobile
{
    class ZXingDefaultOverlay : Box
    {
        private Label topLabel;
        private Label bottomLabel;
        public ZXingDefaultOverlay(EvasObject parent) : base(parent)
        {
            AlignmentX = -1;
            AlignmentY = -1;
            WeightX = 1;
            WeightY = 1;
            InitView();
            BackgroundColor = Color.Transparent;
        }

        private void InitView()
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
            this.PackEnd(topLabel);

            var blankAbove = new ElmSharp.Rectangle(this)
            {
                AlignmentX = -1.0,
                AlignmentY = -1.0,
                WeightX = 1.0,
                WeightY = 0.3,
                Color = Color.Transparent,
            };
            blankAbove.Show();
            this.PackEnd(blankAbove);

            var middleBar = new ElmSharp.Rectangle(this)
            {
                AlignmentX = -1.0,
                AlignmentY = -1.0,
                WeightX = 1.0,
                WeightY = 0.001,
                Color = Color.Red,
            };
            middleBar.Show();
            this.PackEnd(middleBar);

            var blankBelow = new ElmSharp.Rectangle(this)
            {
                AlignmentX = -1.0,
                AlignmentY = -1.0,
                WeightX = 1.0,
                WeightY = 0.3,
                Color = Color.Transparent,
            };
            blankBelow.Show();
            this.PackEnd(blankBelow);

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
            this.PackEnd(bottomLabel);
        }
        public void SetText(string top, string bottom)
        {
            topLabel.Text = top;
            bottomLabel.Text = bottom;
        }
    }
}