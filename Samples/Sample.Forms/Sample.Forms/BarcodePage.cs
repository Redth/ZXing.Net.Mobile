using System;
using Xamarin.Forms;
using System.Threading.Tasks;
using ZXing.Net.Mobile.Forms;

namespace Sample.Forms
{
	public class BarcodePage : ContentPage
	{
		ZXingBarcodeImageView barcode;

		public BarcodePage()
		{
			var stackLayout = new StackLayout
			{
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
			};

			barcode = new ZXingBarcodeImageView
			{
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
				AutomationId = "zxingBarcodeImageView",
			};
			barcode.BarcodeFormat = ZXing.BarcodeFormat.QR_CODE;
			barcode.BarcodeOptions.Width = 300;
			barcode.BarcodeOptions.Height = 300;
			barcode.BarcodeOptions.Margin = 10;
			barcode.BarcodeValue = "ZXing.Net.Mobile";

			var text = new Entry
			{
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "ZXing Sample"
			};
			text.TextChanged += Text_TextChanged;

			stackLayout.Children.Add(barcode);
			stackLayout.Children.Add(text);

			Content = stackLayout;
		}

		void Text_TextChanged(object sender, TextChangedEventArgs e)
			=> barcode.BarcodeValue = e.NewTextValue;
	}
}

