using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using System.ComponentModel;
using System.Reflection;
using Xamarin.Forms.Platform.WinPhone;
using ZXing.Net.Mobile.Forms.WindowsPhone;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.WindowsPhone
{
    //[Preserve(AllMembers = true)]
    public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingScannerControl>
    {
        public static void Init()
        {
            // Force the assembly to load
        }

        ZXingScannerView formsView;

        ZXing.Mobile.ZXingScannerControl zxingControl;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            formsView = Element;

            if (zxingControl == null)
            {
                zxingControl = new ZXing.Mobile.ZXingScannerControl();
                zxingControl.UseCustomOverlay = false;

                formsView.InternalNativeScannerImplementation = zxingControl;
                
                base.SetNativeControl(zxingControl);
            }

            base.OnElementChanged(e);
        }
    }
}

