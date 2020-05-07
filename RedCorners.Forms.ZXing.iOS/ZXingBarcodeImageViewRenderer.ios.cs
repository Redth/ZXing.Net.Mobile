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

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.iOS
{
	[Preserve(AllMembers = true)]
	public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, UIImageView>
	{
		public static void Init()
		{
			var temp = DateTime.Now;
		}

		ZXingBarcodeImageView formsView;
		UIImageView imageView;

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeValue)
				|| e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeOptions)
				|| e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeFormat))
				Regenerate();

			base.OnElementPropertyChanged(sender, e);
		}

		protected override async void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
		{
			formsView = Element;

			if (formsView != null && imageView == null)
			{
				imageView = new UIImageView { ContentMode = UIViewContentMode.ScaleAspectFit };
				SetNativeControl(imageView);
			}

			Regenerate();

			base.OnElementChanged(e);
		}

		void Regenerate()
		{
			BarcodeWriter writer = null;
			string barcodeValue = null;

			if (formsView != null
				&& !string.IsNullOrWhiteSpace(formsView.BarcodeValue)
				&& formsView.BarcodeFormat != BarcodeFormat.All_1D)
			{
				barcodeValue = formsView.BarcodeValue;
				writer = new BarcodeWriter { Format = formsView.BarcodeFormat };
				if (formsView.BarcodeOptions != null)
					writer.Options = formsView.BarcodeOptions;
			}

			// Update or clear out the image depending if we had enough info
			// to instantiate the barcode writer, otherwise null the image
			Device.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var image = writer?.Write(barcodeValue);
					if (imageView != null)
						imageView.Image = image;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to update image: {ex}");
				}
			});
		}
	}
}

