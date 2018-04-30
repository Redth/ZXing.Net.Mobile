using ElmSharp;
using System;

namespace ZXing.Mobile
{
    class ZxingScannerWindow : Window
    {
        public static Action<Result> ScanCompletedHandler;
        public static bool ScanContinuously { get; set; }        
        public static MobileBarcodeScanningOptions ScanningOptions { get; set; }

        public bool IsTorchOn => ZXingMediaView.IsTorchOn;

        public bool UseCustomOverlayView { get; set; }
        public Container CustomOverlayView { get; set; }
        public string TopText { get; internal set; }
        public string BottomText { get; internal set; }

        private ZXingMediaView ZXingMediaView;
        private Background OverlayBackground;

        public ZxingScannerWindow() : base("ZXingScannerWindow")
        {
            TopText = "";
            BottomText = "";
            AvailableRotations = DisplayRotation.Degree_0 | DisplayRotation.Degree_180 | DisplayRotation.Degree_270 | DisplayRotation.Degree_90;
            BackButtonPressed += (s, ex) =>
            {
                ZXingMediaView?.StopScanning();
                Unrealize();
            };
            InitView();            
            EvasObjectEvent showCallback = new EvasObjectEvent(this, EvasObjectCallbackType.Show);
            showCallback.On += (s, e) =>
            {
                StartScanning();
            };            
        }

        private void InitView()
        {
            var mBackground = new Background(this);
            mBackground.Show();
            var mConformant = new Conformant(this);
            mConformant.SetContent(mBackground);
            mConformant.Show();
            mBackground.Show();

            OverlayBackground = new Background(this)
            {
                Color = Color.Transparent,
                BackgroundColor = Color.Transparent,
            };
            OverlayBackground.Show();
            var oConformant = new Conformant(this);
            oConformant.Show();
            oConformant.SetContent(OverlayBackground);

            ZXingMediaView = new ZXingMediaView(this)
            {
                AlignmentX = -1,
                AlignmentY = -1,
                WeightX = 1,
                WeightY = 1,
            };
            ZXingMediaView.Show();
            mBackground.SetContent(ZXingMediaView);

            
        }

        public void StartScanning()
        {
            if (UseCustomOverlayView)
            {
                OverlayBackground.SetContent(CustomOverlayView);
                CustomOverlayView.Show();
            }
            else
            {
                ZXingDefaultOverlay defaultOverlay = new ZXingDefaultOverlay(this);
                defaultOverlay.SetText(TopText, BottomText);
                OverlayBackground.SetContent(defaultOverlay);
                defaultOverlay.Show();
            }
            ZXingMediaView.StartScanning(result => {
                ScanCompletedHandler?.Invoke(result);
                if (!ZxingScannerWindow.ScanContinuously)
                {
                    ZXingMediaView.StopScanning();
                    this.Unrealize();
                }
            }, ScanningOptions);
        }

        public void AutoFocus()
        {
            ZXingMediaView?.AutoFocus();
        }
        public void PauseAnalysis()
        {
            ZXingMediaView?.PauseAnalysis();
        }
        public void ResumeAnalysis()
        {
            ZXingMediaView?.ResumeAnalysis();
        }
        public void Torch(bool on)
        {
            ZXingMediaView?.Torch(on);
        }
        public void ToggleTorch()
        {
            ZXingMediaView?.ToggleTorch();
        }
    }
}
