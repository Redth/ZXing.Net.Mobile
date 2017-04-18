using System;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
    public class MobileBarcodeScanner : MobileBarcodeScannerBase
    {
        NotSupportedException ex = new NotSupportedException ("Use the platform specific implementation instead!");

        public override Task<Result> Scan(MobileBarcodeScanningOptions options)
        {
            throw ex;
        }

        public override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
        {
            throw ex;
        }

        public override void Cancel()
        {
            throw ex;
        }

        public override void AutoFocus ()
        {
            throw ex;
        }

        public override void Torch (bool on)
        {
            throw ex;
        }

        public override void ToggleTorch ()
        {
            throw ex;
        }

        public override void PauseAnalysis()
        {
            throw ex;
        }

        public override void ResumeAnalysis()
        {
            throw ex;
        }

        public override bool IsTorchOn {
            get {
                throw ex;
            }
        }
    }
}

