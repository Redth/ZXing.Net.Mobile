using Cairo;
using Gdk;
using ZXing.Common;
using ZXing.Rendering;

namespace ZXing.Net.Mobile.GTK
{
    public class BitmapRenderer : IBarcodeRenderer<Pixbuf>
    {
        public Pixbuf Render(BitMatrix matrix, BarcodeFormat format, string content)
        {
            var black = new Cairo.Color(0, 0, 0);
            var white = new Cairo.Color(1, 1, 1);
            using (var surface = new ImageSurface(Format.RGB24, matrix.Width, matrix.Height))
            {
                using (var cr = new Context(surface))
                {
                    for (var x = 0; x < matrix.Width; x++)
                    {
                        for (var y = 0; y < matrix.Height; y++)
                        {
                            SetSourceColor(cr, matrix[x, y] ? black : white);
                            cr.MoveTo(x, y);
                            cr.LineTo(x + 1, y);
                            cr.Stroke();
                        }
                    }
                    return new Pixbuf(surface.Data,
                        Colorspace.Rgb,
                        true,
                        8,
                        matrix.Width,
                        matrix.Height,
                        surface.Stride);
                }
            }

        }

        private void SetSourceColor(Context context, Cairo.Color color)
        {
            context.SetSourceRGB(color.R, color.G, color.B);
        }

        public Pixbuf Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
        {
            return Render(matrix, format, content);
        }
    }
}