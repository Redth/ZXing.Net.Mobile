using ElmSharp;
using Tizen.Multimedia.Vision;
using ZXing.Common;
using ZXing.Rendering;

namespace ZXing.Mobile
{
	class BitmapRenderer : IBarcodeRenderer<EvasImage>
	{
        readonly EvasObject nativeParent;

		public BitmapRenderer(EvasObject nativeParent) : base()
			=> this.nativeParent = nativeParent;

		public EvasImage Render(BitMatrix matrix, BarcodeFormat format, string content)
			=> Render(matrix, format, content, new EncodingOptions());

		public EvasImage Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
		{
			var type = BarcodeType.Code128;
			switch (format)
			{
				case BarcodeFormat.CODE_128:
					type = BarcodeType.Code128;
					break;
				case BarcodeFormat.CODE_39:
					type = BarcodeType.Code39;
					break;
				case BarcodeFormat.EAN_13:
					type = BarcodeType.Ean13;
					break;
				case BarcodeFormat.EAN_8:
					type = BarcodeType.Ean8;
					break;
				case BarcodeFormat.ITF:
					type = BarcodeType.I25;
					break;
				case BarcodeFormat.QR_CODE:
					type = BarcodeType.QR;
					break;
				case BarcodeFormat.UPC_A:
					type = BarcodeType.UpcA;
					break;
				case BarcodeFormat.UPC_E:
					type = BarcodeType.UpcE;
					break;
			}

			var path = Tizen.Applications.Application.Current.DirectoryInfo.Cache + "temporary_barcode";
			var barcodeImageConfiguration = new BarcodeImageConfiguration(options.Width, options.Height, path, BarcodeImageFormat.Png);
			path += ".png";

			if (type == BarcodeType.QR)
			{
				var qrConfig = new QrConfiguration(QrMode.Utf8, ErrorCorrectionLevel.Medium, 10);
				BarcodeGenerator.GenerateImage(content, qrConfig, barcodeImageConfiguration);
			}
			else
			{
				BarcodeGenerator.GenerateImage(content, BarcodeType.Code128, barcodeImageConfiguration);
			}

			var evasImage = new EvasImage(nativeParent)
			{
				AlignmentX = 0.5,
				AlignmentY = 0.5,
				MinimumWidth = options.Width,
				MinimumHeight = options.Height,
				File = path,
				IsFilled = true,
			};
			return evasImage;
		}
	}
}
