using System;
using Android.Content.Res;

namespace ZXing.Net.Mobile.Android
{
    public class PlanarNV21LuminanceSource : BaseLuminanceSource
    {
        DeviceOrientationData orientationData = null;

        public override bool CropSupported => false;

        public override bool RotateSupported => true;


        public PlanarNV21LuminanceSource(byte[] nv21Data, int width, int height, DeviceOrientationData orientationData, bool useOrientationDataToCorrect)
            : base(width, height)
        {
            this.orientationData = orientationData;
            base.luminances = nv21Data;
            Width = width;
            Height = height;

            if (useOrientationDataToCorrect && orientationData == null)
                throw new ArgumentNullException($"{orientationData} can't be null when correction is requested");

            if (orientationData != null && useOrientationDataToCorrect)
                ValidateRotation();
        }

        public PlanarNV21LuminanceSource(byte[] nv21Data, int width, int height)
            : this(nv21Data, width, height, null, false)
        {
        }

        void ValidateRotation()
        {
            if (orientationData.SensorRotation % 90 != 0) // we don't support weird sensor orientations 
            {
                return;
            }

            var rotateBy = 0;
            if (orientationData.OrientationMode == Orientation.Landscape)
            {
                if (orientationData.DeviceOrientation >= 180) // Navigation on the left, Header on the left (270°)
                {
                    rotateBy = 270; // 270 + 90 = 360 || 360 zeros out so nothing to do
                }
                else if (orientationData.DeviceOrientation < 180) // Navigation on the left, Header on the Right (90°)
                {
                    rotateBy = 90;
                }
            }
            else
            {
                if (orientationData.DeviceOrientation > 90) // Upside down and not landscape mode
                {
                    rotateBy = 180;
                }
            }

            rotateBy += orientationData.SensorRotation;
            rotateBy %= 360;

            var rotateResult = RotateNV21(luminances, Width, Height, rotateBy);
            luminances = rotateResult.NV21;
            Width = rotateResult.Width;
            Height = rotateResult.Height;
        }

        protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
            => new PlanarNV21LuminanceSource(newLuminances, width, height, orientationData, false);

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
            var yflip = rotation >= 180;

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
