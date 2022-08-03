using System;

namespace ZXing.Net.Mobile.Android
{
    public class PlanarNV21LuminanceSource : BaseLuminanceSource
    {
        int sensorRotation = 0;

        public override bool CropSupported => false;

        public override bool RotateSupported => true;

        public int CurrentRotation { get; private set; }

        public PlanarNV21LuminanceSource(int sensorRotation, byte[] nv21Data, int width, int height, bool correctToSensorOrientation = true)
            : base(width, height)
        {
            this.sensorRotation = sensorRotation;
            base.luminances = nv21Data;
            Width = width;
            Height = height;

            if (correctToSensorOrientation)
                ValidateRotation();
        }

        void ValidateRotation()
        {
            if (sensorRotation % 90 != 0) // we don't support weird sensor orientations 
            {
                return;
            }

            if (sensorRotation != CurrentRotation)
            {
                var rotateResult = RotateNV21(luminances, Width, Height, sensorRotation);
                luminances = rotateResult.NV21;
                Width = rotateResult.Width;
                Height = rotateResult.Height;

                CurrentRotation = sensorRotation;
            }
        }

        protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
            => new PlanarNV21LuminanceSource(0, newLuminances, width, height);

        public override LuminanceSource rotateCounterClockwise() => GetRotatedLuminanceSource(270);

        public LuminanceSource RotateClockwise() => GetRotatedLuminanceSource(90);

        public LuminanceSource Mirror() => GetRotatedLuminanceSource(180);

        LuminanceSource GetRotatedLuminanceSource(int rotation)
        {
            var rotateResult = RotateNV21(luminances, Width, Height, rotation);
            return CreateLuminanceSource(rotateResult.NV21, rotateResult.Width, rotateResult.Height);
        }

        // https://stackoverflow.com/questions/6853401/camera-pixels-rotated/31425229#31425229
        public static (byte[] NV21, int Width, int Height) RotateNV21(byte[] nv21, int width, int height, int rotation)
        {
            if (rotation == 0)
                return (nv21, width, height);

            if (rotation % 90 != 0 || rotation < 0 || rotation > 270)
            {
                throw new ArgumentException("0 <= rotation < 360, rotation % 90 == 0");
            }

            var output = new byte[nv21.Length];
            var frameSize = width * height;
            var swap = rotation % 180 != 0;
            var xflip = rotation % 270 != 0;
            var yflip = rotation > 180;

            for (var row = 0; row < height; row++)
            {
                for (var col = 0; col < width; col++)
                {
                    var yInPos = row * width + col;
                    var uInPos = frameSize + (row >> 1) * width + (col & ~1);
                    var vInPos = uInPos + 1;

                    var widthOut = swap ? height : width;
                    var heightOut = swap ? width : height;
                    var colSwapped = swap ? row : col;
                    var rowSwapped = swap ? col : row;
                    var colOut = xflip ? widthOut - colSwapped - 1 : colSwapped;
                    var rowOut = yflip ? heightOut - rowSwapped - 1 : rowSwapped;

                    var yOutPos = rowOut * widthOut + colOut;
                    var uOutPos = frameSize + (rowOut >> 1) * widthOut + (colOut & ~1);
                    var vOutPos = uOutPos + 1;

                    output[yOutPos] = (byte)(0xff & nv21[yInPos]);
                    output[uOutPos] = (byte)(0xff & nv21[uInPos]);
                    output[vOutPos] = (byte)(0xff & nv21[vInPos]);
                }
            }

            return (output, swap ? height : width, swap ? width : height);
        }
    }
}
