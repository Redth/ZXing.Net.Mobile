﻿using System;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.iOS;
using System.ComponentModel;
using System.Reflection;
using ZXing.Mobile;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.iOS;
using Foundation;
using UIKit;

[assembly:ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.iOS
{
    [Preserve(AllMembers = true)]
    public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, UIImageView>
    {       
        public static void Init ()
        {
        }

        ZXingBarcodeImageView formsView;
        UIImageView imageView;

        protected override async void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
        {
            formsView = Element;

            if (imageView == null) {

                imageView = new UIImageView ();

                base.SetNativeControl (imageView);     
            }

            if (formsView != null && formsView.BarcodeValue != null)
            {
                var writer = new ZXing.Mobile.BarcodeWriter();

                if (formsView != null && formsView.BarcodeOptions != null)
                    writer.Options = formsView.BarcodeOptions;
                if (formsView != null && formsView.BarcodeFormat != null)
                    writer.Format = formsView.BarcodeFormat;

                var value = formsView != null ? formsView.BarcodeValue : string.Empty;

                var image = writer.Write(value);

                imageView.Image = image;
            }

            base.OnElementChanged (e);
        }
    }
}
