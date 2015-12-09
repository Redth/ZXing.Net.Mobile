using System;
using Xamarin.Forms;
using ZXing.Mobile;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingScannerView : View
    {
        public delegate void ScanResultDelegate (ZXing.Result result);
        public event ScanResultDelegate OnScanResult;

        Action<MobileBarcodeScanningOptions> startScanningHandler;
        Action stopScanningHandler;
        Action toggleFlashHandler;
        Func<bool> getFlashHandler;
        Action<bool> setFlashHandler;
        Action<int, int> autoFocusHandler;

        public ZXingScannerView ()
        {
            //IsClippedToBounds = true;
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;
        }

        public void StartScanning (MobileBarcodeScanningOptions options = null)
        {
            var h = startScanningHandler;
            if (h != null)
                h (options ?? MobileBarcodeScanningOptions.Default);
        }

        public void StopScanning ()
        {
            var h = stopScanningHandler;
            if (h != null)
                h ();
        }

        public void ToggleFlash ()
        {
            var h = toggleFlashHandler;
            if (h != null)
                h ();
        }

        public bool Flash
        {
            get {
                var h = getFlashHandler;
                if (h != null)
                    return h ();

                return false;
            } 
            set {
                var h = setFlashHandler;
                if (h != null)
                    h (value);
            }
        }

        public void AutoFocus ()
        {
            var h = autoFocusHandler;
            if (h != null)
                h (-1, -1);
        }

        public void AutoFocus (int x, int y)
        {
            var h = autoFocusHandler;
            if (h != null)
                h (x, y);
        }

        public void SetInternalHandlers (
            Action<MobileBarcodeScanningOptions> startScanning,
            Action stopScanning,
            Action toggleFlash,
            Func<bool> getFlash,
            Action<bool> setFlash,
            Action<int, int> autoFocus) {
            startScanningHandler = startScanning;
            stopScanningHandler = stopScanning;
            toggleFlashHandler = toggleFlash;
            getFlashHandler = getFlash;
            setFlashHandler = setFlash;
            autoFocusHandler = autoFocus;
        }

        public void RaiseScanResult (ZXing.Result result)
        {
            var e = this.OnScanResult;
            if (e != null)
                e (result);
        }
    }
}

