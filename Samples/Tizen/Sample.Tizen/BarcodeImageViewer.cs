using ElmSharp;

namespace Sample.Tizen
{
    public class BarcodeImageViewer : Window
    {
        public BarcodeImageViewer() : base("BarcodeImageViewer")
        {
            AvailableRotations = DisplayRotation.Degree_0 | DisplayRotation.Degree_180 | DisplayRotation.Degree_270 | DisplayRotation.Degree_90;

            this.BackButtonPressed += (s, e) =>
            {
                this.Unrealize();
            };

            var backgournd = new Background(this)
            {
                BackgroundColor = Color.White,
            };
            var conformant = new Conformant(this);
            conformant.Show();
            conformant.SetContent(backgournd);

            var box = new Box(this)
            {
                AlignmentX = -1,
                AlignmentY = -1,
                WeightX = 1,
                WeightY = 1,
            };
            backgournd.SetContent(box);

            var barcodeWriter = new ZXing.Mobile.BarcodeWriter(this)
            {
                Format = ZXing.BarcodeFormat.QR_CODE,                
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 300,
                    Height = 300
                }
            };
            var barcode = barcodeWriter.Write("ZXing.Net.Mobile");
            barcode.Show();
            box.PackEnd(barcode);
            box.Show();
        }
    }
}
