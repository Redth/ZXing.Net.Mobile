using System;
using System.ComponentModel;
using System.Threading.Tasks;

using Android.App;
using Android.Runtime;
using Android.Views;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;

using MyView = ZXing.Mobile.ZXingTextureView;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
    [Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, MyView>
    {       
        public static void Init ()
        {
            // Keep linker from stripping empty method
            var temp = DateTime.Now;
        }

        protected ZXingScannerView formsView;

        protected MyView view;
        internal Task<bool> requestPermissionsTask;

        protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            base.OnElementChanged (e);

            formsView = Element;

            if (view == null) {

                // Process requests for autofocus
                formsView.AutoFocusRequested += (x, y) => {
                    if (view != null) {
                        if (x < 0 && y < 0)
                            view.AutoFocus ();
                        else
                            view.AutoFocus (x, y);
                    }
                };

                var activity = Context as Activity;

                if (activity != null)                
                    await ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync (activity);
                
                view = new MyView(Xamarin.Forms.Forms.Context, formsView.Options);
				view.LayoutParameters = new LayoutParams (LayoutParams.MatchParent, LayoutParams.MatchParent);

                base.SetNativeControl (view);

                if (formsView.IsScanning)
                    view.StartScanning(formsView.RaiseScanResult, formsView.Options);

                if (!formsView.IsAnalyzing)
                    view.PauseAnalysis ();

                if (formsView.IsTorchOn)
                    view.Torch (true);
            }
        }

        protected override void OnElementPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged (sender, e);

            if (view == null)
                return;
            
            switch (e.PropertyName) {
            case nameof (ZXingScannerView.IsTorchOn):
                view.Torch (formsView.IsTorchOn);
                break;
            case nameof (ZXingScannerView.IsScanning):
                if (formsView.IsScanning)
                    view.StartScanning (formsView.RaiseScanResult, formsView.Options);
                else
                    view.StopScanning ();
                break;
            case nameof (ZXingScannerView.IsAnalyzing):
                if (formsView.IsAnalyzing)
                    view.ResumeAnalysis ();
                else
                    view.PauseAnalysis ();
                break;
            } 
        }

        public override bool OnTouchEvent (MotionEvent e)
        {
            var x = e.GetX ();            
            var y = e.GetY ();

            if (view != null) {
                view.AutoFocus ((int)x, (int)y);
                System.Diagnostics.Debug.WriteLine ("Touch: x={0}, y={1}", x, y);
            }
            return base.OnTouchEvent (e);
        }
    }
}

