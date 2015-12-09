using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;
using Android.Runtime;
using Android.App;
using Xamarin.Forms.Platform.Android;
using Android.Views;
using System.ComponentModel;
using System.Reflection;
using Android.Widget;

[assembly:ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
    [Preserve(AllMembers = true)]
    public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingSurfaceView>
    {       
        ZXingScannerView formsView;

        ZXing.Mobile.ZXingSurfaceView zxingSurface;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            formsView = Element;

            if (zxingSurface == null) {
                
                zxingSurface = new ZXing.Mobile.ZXingSurfaceView (Xamarin.Forms.Forms.Context as Activity);
                zxingSurface.LayoutParameters = new LayoutParams (LayoutParams.MatchParent, LayoutParams.MatchParent);

                formsView.SetInternalHandlers (options => {
                    // Start Scanning
                    var opt = options ?? ZXing.Mobile.MobileBarcodeScanningOptions.Default;
                    zxingSurface.StartScanning (opt, formsView.RaiseScanResult);
                }, () => {
                    // Stop Scanning
                    zxingSurface.StopScanning ();
                }, () => {
                    // Toggle Flash
                    zxingSurface.ToggleTorch ();
                }, () => {
                    // Get Flash
                    return zxingSurface.IsTorchOn;
                }, (on) => {
                    // Set Flash Handler
                    zxingSurface.SetTorch (on);
                }, (x, y) => {
                    // Autofocus
                    zxingSurface.AutoFocus ();
                });
                  
                base.SetNativeControl (zxingSurface);                
            }

            base.OnElementChanged (e);
        }

        public override bool OnTouchEvent (MotionEvent e)
        {
            var x = e.GetX ();            
            var y = e.GetY ();

            zxingSurface.AutoFocus ();
            System.Diagnostics.Debug.WriteLine ("Touch: x={0}, y={1}", x, y);
            return base.OnTouchEvent (e);
        }
    }
}

