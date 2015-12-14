using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using Xamarin.Forms.Platform.iOS;
using System.ComponentModel;
using System.Reflection;
using Foundation;
using ZXing.Net.Mobile.Forms.iOS;

[assembly:ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.iOS
{
    [Preserve(AllMembers = true)]
    public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingScannerView>
    {   
        // No-op to be called from app to prevent linker from stripping this out    
        public static void Init ()
        {
        }

        ZXingScannerView formsView;

        ZXing.Mobile.ZXingScannerView zxingView;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            formsView = Element;

            if (zxingView == null) {

                zxingView = new ZXing.Mobile.ZXingScannerView ();
                zxingView.UseCustomOverlayView = true;

                formsView.InternalNativeScannerImplementation = zxingView;

                base.SetNativeControl (zxingView);                
            }

            base.OnElementChanged (e);
        }

        public override void TouchesEnded (NSSet touches, UIKit.UIEvent evt)
        {
            base.TouchesEnded (touches, evt);

            zxingView.AutoFocus ();
        }
    }
}

