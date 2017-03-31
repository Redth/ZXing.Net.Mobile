using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Net.Mobile
{
    public class NotSupported
    {
        // Windows Phone 8.1 Native and Windows 8 RT are not supported

        // Microsoft left out the ability to (easily) marshal Camera Preview frames to managed code
        // Which is what ZXing.Net.Mobile needs to work

        // You should upgrade your wpa81 / win8 projects to use Windows Universal (UWP) instead!
    }
}

namespace ZXing.Mobile
{
    public class MobileBarcodeScanner : MobileBarcodeScannerBase
    {
        NotSupportedException ex = new NotSupportedException("Windows Phone 8.1 Native (wpa81) and Windows 8 Store (win8) are not supported, please use Windows Universal (UWP) instead!");

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

        public override void AutoFocus()
        {
            throw ex;
        }

        public override void Torch(bool on)
        {
            throw ex;
        }

        public override void ToggleTorch()
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

        public override bool IsTorchOn
        {
            get
            {
                throw ex;
            }
        }
    }
 }


