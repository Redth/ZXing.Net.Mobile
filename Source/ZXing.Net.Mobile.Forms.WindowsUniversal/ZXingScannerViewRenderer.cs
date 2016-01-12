using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.WindowsUniversal;
using Xamarin.Forms.Platform.UWP;
using System.ComponentModel;
using System.Reflection;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsUniversal
{
    //[Preserve(AllMembers = true)]
    public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingScannerControl>
    {
        public static void Init ()
        {
            // Cause the assembly to load
        }

        ZXingScannerView formsView;

        ZXing.Mobile.ZXingScannerControl zxingControl;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            formsView = Element;

            if (zxingControl == null)
            {
                zxingControl = new ZXing.Mobile.ZXingScannerControl();
                zxingControl.UseCustomOverlay = true;

                formsView.InternalNativeScannerImplementation = zxingControl;

                base.SetNativeControl(zxingControl);
            }

            base.OnElementChanged(e);
        }
    }
}

