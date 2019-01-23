using System;
using Gdk;
using Gtk;
using ZXing.Net.Mobile.GTK;

public partial class MainWindow : Gtk.Window
{
    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();
        var barcodeWriter = new BarcodeWriter
        {
            Format = ZXing.BarcodeFormat.QR_CODE,
            Options = new ZXing.Common.EncodingOptions
            {
                Width = 300,
                Height = 300,
                Margin = 10
            }
        };

        var barcode = barcodeWriter.Write("ZXing.Net.Mobile");
        barcodeImage.Pixbuf = barcode;
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }
}
