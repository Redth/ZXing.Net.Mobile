namespace ZXing.Mobile.CameraAccess
{
    public class CapturedImageData
    {
        public byte[] Matrix { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public CapturedImageData(byte[] matrix, int width, int height)
        {
            Matrix = matrix;
            Width = width;
            Height = height;
        }
    }
}
