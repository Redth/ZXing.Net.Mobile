namespace ZXing.Mobile.Detectors
{
    public class ZXingDetector : IDetector
    {
        private readonly BarcodeReader _detector;

        public ZXingDetector()
        {
            _detector = new BarcodeReader(null, null, null,
                (p, w, h, f) => new PlanarYUVLuminanceSource(p, w, h, 0, 0, w, h, false));
        }

        public bool Init(MobileBarcodeScanningOptions scanningOptions)
        {
            if (scanningOptions.TryHarder.HasValue)
            {
                _detector.Options.TryHarder = scanningOptions.TryHarder.Value;
            }
            if (scanningOptions.PureBarcode.HasValue)
            {
                _detector.Options.PureBarcode = scanningOptions.PureBarcode.Value;
            }
            if (!string.IsNullOrEmpty(scanningOptions.CharacterSet))
            {
                _detector.Options.CharacterSet = scanningOptions.CharacterSet;
            }
            if (scanningOptions.TryInverted.HasValue)
            {
                _detector.TryInverted = scanningOptions.TryInverted.Value;
            }

            if (scanningOptions.PossibleFormats != null && scanningOptions.PossibleFormats.Count > 0)
            {
                _detector.Options.PossibleFormats = scanningOptions.PossibleFormats.ToArray();
            }

            return true;
        }

        public Result Decode(byte[] bytes, int width, int height)
        {
            return _detector.Decode(bytes, width, height, RGBLuminanceSource.BitmapFormat.Unknown);
        }
    }
}