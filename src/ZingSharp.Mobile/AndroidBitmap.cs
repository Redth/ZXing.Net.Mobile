using System;
using System.Reflection;
using System.IO;

namespace System.Drawing
{
	public abstract class Image : IDisposable
    {
        public abstract int Width{get;}
        public abstract int Height{get;}

        public Size Size{ get{ return new Size(Width, Height); }}

        public void Dispose()
        {
        }
    }

    public class Bitmap : Image
    {
        Android.Graphics.Bitmap ABitmap;

        public Bitmap (int w, int h)
        {
            ABitmap = Android.Graphics.Bitmap.CreateBitmap (w, h, Android.Graphics.Bitmap.Config.Argb8888);
        }

		public Bitmap (Android.Graphics.Bitmap androidBitmap)
		{
			ABitmap = androidBitmap;
		}

		public Color GetPixel (int x, int y)
		{
			return Color.FromArgb (ABitmap.GetPixel (x, y));
		}

        public override int Width{
            get{ return ABitmap.Width; }
        }
        public override int Height{
            get{ return ABitmap.Height; }
        }

    }

}

