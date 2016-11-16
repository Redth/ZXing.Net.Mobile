using System;
using System.ComponentModel;
using System.Threading.Tasks;

using Android.App;
using Android.Runtime;
using Android.Views;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
    [Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingTextureView>
    {       
        public static void Init ()
        {
            // Keep linker from stripping empty method
            var temp = DateTime.Now;
        }

        protected ZXingScannerView formsView;

		protected ZXingTextureView zxingTexture;
        internal Task<bool> requestPermissionsTask;

        protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            base.OnElementChanged (e);

            formsView = Element;

            if (zxingTexture == null) {

                // Process requests for autofocus
                formsView.AutoFocusRequested += (x, y) => {
                    if (zxingTexture != null) {
                        if (x < 0 && y < 0)
                            zxingTexture.AutoFocus ();
                        else
                            zxingTexture.AutoFocus (x, y);
                    }
                };

                var activity = Context as Activity;

                if (activity != null)                
                    await ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync (activity);
                
				zxingTexture = new ZXingTextureView(Xamarin.Forms.Forms.Context);
				zxingTexture.LayoutParameters = new LayoutParams (LayoutParams.MatchParent, LayoutParams.MatchParent);

                base.SetNativeControl (zxingTexture);

                if (formsView.IsScanning)
                    zxingTexture.StartScanning(formsView.RaiseScanResult, formsView.Options);

                if (!formsView.IsAnalyzing)
                    zxingTexture.PauseAnalysis ();

                if (formsView.IsTorchOn)
                    zxingTexture.Torch (true);
            }
        }

        protected override void OnElementPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged (sender, e);

            if (zxingTexture == null)
                return;
            
            switch (e.PropertyName) {
            case nameof (ZXingScannerView.IsTorchOn):
                zxingTexture.Torch (formsView.IsTorchOn);
                break;
            case nameof (ZXingScannerView.IsScanning):
                if (formsView.IsScanning)
                    zxingTexture.StartScanning (formsView.RaiseScanResult, formsView.Options);
                else
                    zxingTexture.StopScanning ();
                break;
            case nameof (ZXingScannerView.IsAnalyzing):
                if (formsView.IsAnalyzing)
                    zxingTexture.ResumeAnalysis ();
                else
                    zxingTexture.PauseAnalysis ();
                break;
            } 
        }

        public override bool OnTouchEvent (MotionEvent e)
        {
            var x = e.GetX ();            
            var y = e.GetY ();

            if (zxingTexture != null) {
                zxingTexture.AutoFocus ((int)x, (int)y);
                System.Diagnostics.Debug.WriteLine ("Touch: x={0}, y={1}", x, y);
            }
            return base.OnTouchEvent (e);
        }
    }
}

