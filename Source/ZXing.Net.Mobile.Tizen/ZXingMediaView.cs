using ElmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tizen.Multimedia;

namespace ZXing.Mobile
{
    public class ZXingMediaView : MediaView, IScannerView
    {
        private ZXingScannerCammera ZXingScannerCammera;
        private EvasObjectEvent showCallback;
        public ZXingMediaView(EvasObject parent) : base(parent)
        {
            AlignmentX = -1;
            AlignmentY = -1;
            WeightX = 1;
            WeightY = 1;
            ZXingScannerCammera = new ZXingScannerCammera(CameraDevice.Rear, this);

            showCallback = new EvasObjectEvent(this, EvasObjectCallbackType.Show);
            showCallback.On += (s, e) =>
            {
                if (ZXingScannerCammera == null)
                    ZXingScannerCammera = new ZXingScannerCammera(CameraDevice.Rear, this);
            };

        }
        public bool IsTorchOn => ZXingScannerCammera.IsTorchOn;

        public bool IsAnalyzing { get; private set; }

        public bool HasTorch => ZXingScannerCammera.HasTorch;

        public void AutoFocus()
        {
            ZXingScannerCammera?.AutoFocus();
        }

        public void AutoFocus(int x, int y)
        {
            ZXingScannerCammera?.AutoFocus(x, y);
        }

        public void PauseAnalysis()
        {
            ZXingScannerCammera?.PauseAnalysis();
        }

        public void ResumeAnalysis()
        {
            ZXingScannerCammera?.ResumeAnalysis();
        }

        public void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
        {
            IsAnalyzing = true;
            Show();
            ZXingScannerCammera.scanningOptions = options;
            ZXingScannerCammera?.Scan(scanResultHandler);
            IsAnalyzing = false;
        }

        public void StopScanning()
        {
            ZXingScannerCammera?.StopScanning();
        }

        public void ToggleTorch()
        {
            ZXingScannerCammera?.ToggleTorch();
        }

        public void Torch(bool on)
        {
            ZXingScannerCammera?.Torch(on);
        }
    }
}
