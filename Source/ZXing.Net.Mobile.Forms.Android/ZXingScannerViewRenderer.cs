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

[assembly:ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
    [Preserve(AllMembers = true)]
    public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingSurfaceView>
    {       
        ZXingScannerView formsView;
        IntermediaryScanner intermediaryScanner;

        internal ZXingSurfaceView zxingSurface;
        internal Task<bool> requestPermissionsTask;

        internal TaskCompletionSource<bool> finishedSetup;

        protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            formsView = Element;


            if (zxingSurface == null) {
                finishedSetup = new TaskCompletionSource<bool> ();

                // We'll proxy our requests through an intermediate layer that can intercept
                // the scanning so it can ask for permission
                intermediaryScanner = new IntermediaryScanner {
                    Parent = this                
                };
                formsView.InternalNativeScannerImplementation = intermediaryScanner;

                var activity = Context as Activity;

                if (activity != null)                
                    await PermissionsHandler.RequestPermissions (activity);
                
                zxingSurface = new ZXingSurfaceView (Xamarin.Forms.Forms.Context as Activity);
                zxingSurface.LayoutParameters = new LayoutParams (LayoutParams.MatchParent, LayoutParams.MatchParent);
                  
                base.SetNativeControl (zxingSurface);     

                finishedSetup.TrySetResult (true);
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

        class IntermediaryScanner : IScannerView
        {
            public ZXingScannerViewRenderer Parent { get;set; }
                
            #region IScannerView implementation
            public async void StartScanning (Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
            {
                // Wait for permission request to complete
                if (Parent.finishedSetup != null)
                    await Parent.finishedSetup.Task;

                if (Parent.zxingSurface != null)
                    Parent.zxingSurface.StartScanning (scanResultHandler, options);                
            }

            public void StopScanning ()
            {
                if (Parent.zxingSurface != null)
                    Parent.zxingSurface.StopScanning ();
            }

            public void PauseAnalysis ()
            {
                if (Parent.zxingSurface != null)
                    Parent.zxingSurface.PauseAnalysis ();
            }

            public void ResumeAnalysis ()
            {
                if (Parent.zxingSurface != null)
                    Parent.zxingSurface.ResumeAnalysis ();
            }

            public void Torch (bool on)
            {
                if (Parent.zxingSurface != null)
                    Parent.zxingSurface.Torch (on);
            }

            public void AutoFocus ()
            {
                if (Parent.zxingSurface != null)
                    Parent.zxingSurface.AutoFocus ();
            }

            public void AutoFocus (int x, int y)
            {
                if (Parent.zxingSurface != null)
                    Parent.zxingSurface.AutoFocus (x, y);
            }

            public void ToggleTorch ()
            {
                if (Parent.zxingSurface != null)
                    Parent.zxingSurface.ToggleTorch ();
            }

            public bool IsTorchOn { get { return Parent.zxingSurface != null && Parent.zxingSurface.IsTorchOn; } }

            public bool IsAnalyzing { get { return Parent.zxingSurface != null && Parent.zxingSurface.IsAnalyzing; } }

            public bool HasTorch { get { return Parent.zxingSurface != null && Parent.zxingSurface.HasTorch; } }                
            #endregion
        }
    }
}

