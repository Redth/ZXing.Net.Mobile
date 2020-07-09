namespace ZXing.Mobile.iOS.Helpers
{
    class Rect
    {
        public readonly int Left;
        public readonly int Right;
        public readonly int Top;
        public readonly int Bottom;

        public readonly int Width;
        public readonly int Height;

        public Rect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;

            Width = Right - Left;
            Height = Bottom - Top;
        }

        public bool Outside(int x, int y)
        {
            return Left > x || Right <= x || Top > y || Bottom <= y;
        }
    }
}