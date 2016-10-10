namespace ZXing.Mobile.Detectors
{
    public interface IDetector
    {
        bool Init(MobileBarcodeScanningOptions scanningOptions);

        ZXing.Result Decode(byte[] bytes, int width, int height);
    }
}