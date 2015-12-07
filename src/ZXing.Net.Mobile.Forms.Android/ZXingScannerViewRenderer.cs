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
       
        ZXing.Mobile.ZXingSurfaceView zxingSurface;

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
        {
            zxingSurface = new ZXing.Mobile.ZXingSurfaceView (Xamarin.Forms.Forms.Context as Activity);
            zxingSurface.StartScanning (r => {
                Console.WriteLine (r);
            });

            SetNativeControl (zxingSurface);
        }
    }
}

