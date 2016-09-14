namespace ZXing.Mobile.Detectors
{
    public interface IDetector
    {
        void Init(MobileBarcodeScanningOptions scanningOptions);

        ZXing.Result Decode(byte[] bytes, int width, int height);
    }
}