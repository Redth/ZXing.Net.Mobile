using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using com.google.zxing;

namespace ZxingSharp.Mobile
{
    public class PhotoCameraLuminanceSource : LuminanceSource
    {
        public byte[] PreviewBufferY { get; private set; }

        public PhotoCameraLuminanceSource(int width, int height)
            : base(width, height)
        {
            PreviewBufferY = new byte[width * height];
        }

        public override sbyte[] Matrix
        {
            get { return (sbyte[])(Array)PreviewBufferY; }
        }

        public override sbyte[] getRow(int y, sbyte[] row)
        {
            if (row == null || row.Length < Width)
            {
                row = new sbyte[Width];
            }

            for (int i = 0; i < Height; i++)
                row[i] = (sbyte)PreviewBufferY[i * Width + y];

            return row;
        }
    }
}