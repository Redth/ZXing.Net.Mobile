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
using ZXing.Mobile;
using System.Threading.Tasks;
using System.Linq.Expressions;

[assembly:ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
    [Preserve(AllMembers = true)]
    public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingSurfaceView>
    {       
        public ZXingScannerViewRenderer () : base ()
        {
        }

        public static void Init ()
        {
            // Keep linker from stripping empty method
            var temp = DateTime.Now;
        }

        protected ZXingScannerView formsView;

        protected ZXingSurfaceView zxingSurface;
        internal Task<bool> requestPermissionsTask;

        protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            base.OnElementChanged (e);

            formsView = Element;

            if (zxingSurface == null) {

                // Process requests for autofocus
                formsView.AutoFocusRequested += (x, y) => {
                    if (zxingSurface != null) {
                        if (x < 0 && y < 0)
                            zxingSurface.AutoFocus ();
                        else
                            zxingSurface.AutoFocus (x, y);
                    }
                };

                var activity = Context as Activity;

                if (activity != null)                
                    await ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync (activity);
                
                zxingSurface = new ZXingSurfaceView (Xamarin.Forms.Forms.Context as Activity, formsView.Options);
                zxingSurface.LayoutParameters = new LayoutParams (LayoutParams.MatchParent, LayoutParams.MatchParent);

                base.SetNativeControl (zxingSurface);

                if (formsView.IsScanning)
                    zxingSurface.StartScanning(formsView.RaiseScanResult, formsView.Options);

                if (!formsView.IsAnalyzing)
                    zxingSurface.PauseAnalysis ();

                if (formsView.IsTorchOn)
                    zxingSurface.Torch (true);
            }
        }

        protected override void OnElementPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged (sender, e);

            if (zxingSurface == null)
                return;
            
            switch (e.PropertyName) {
            case nameof (ZXingScannerView.IsTorchOn):
                zxingSurface.Torch (formsView.IsTorchOn);
                break;
            case nameof (ZXingScannerView.IsScanning):
                if (formsView.IsScanning)
                    zxingSurface.StartScanning (formsView.RaiseScanResult, formsView.Options);
                else
                    zxingSurface.StopScanning ();
                break;
            case nameof (ZXingScannerView.IsAnalyzing):
                if (formsView.IsAnalyzing)
                    zxingSurface.ResumeAnalysis ();
                else
                    zxingSurface.PauseAnalysis ();
                break;
            } 
        }

        public override bool OnTouchEvent (MotionEvent e)
        {
            var x = e.GetX ();            
            var y = e.GetY ();

            if (zxingSurface != null) {
                zxingSurface.AutoFocus ((int)x, (int)y);
                System.Diagnostics.Debug.WriteLine ("Touch: x={0}, y={1}", x, y);
            }
            return base.OnTouchEvent (e);
        }
    }
}

