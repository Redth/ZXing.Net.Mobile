# zxing.MonoTouch
ZXing (pronounced "zebra crossing") is an open-source, multi-format 1D/2D barcode image processing library implemented in Java. Our focus is on using the built-in camera on mobile phones to photograph and decode barcodes on the device, without communicating with a server.
This project is built from the official csharp port from SVN and may be missing functionality.

## Usage
A simple example of using zxing.MonoDroid might look like this:

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net;
  using MonoTouch.Foundation;
  using MonoTouch.UIKit;
  using com.google.zxing;
  using com.google.zxing.common;

  namespace Camera.iPhone
  {

    public class Application
    {
      static void Main (string[] args)
      {
        UIApplication.Main (args);
      }
    }

    // The name AppDelegate is referenced in the MainWindow.xib file.
    public partial class AppDelegate : UIApplicationDelegate
    {
      // This method is invoked when the application has loaded its UI and its ready to run
      public override bool FinishedLaunching (UIApplication app, NSDictionary options)
      {
        // If you have defined a view, add it here:
        // window.AddSubview (navigationController.View);

        window.MakeKeyAndVisible ();

        try
        {
          var wc = new WebClient();
          var uri = new Uri("http://www.theipadfan.com/wp-content/uploads/2010/07/barcode.png");
          wc.DownloadFile(uri,"barcode.png");

          UIImage image = UIImage.FromFile("barcode.png");
          var srcbitmap = new System.Drawing.Bitmap(image);

          Reader barcodeReader = new MultiFormatReader();
                  LuminanceSource source = new RGBLuminanceSource(srcbitmap, (int)image.Size.Width, (int)image.Size.Height);
                  BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(source));
                  var result = barcodeReader.decode(bitmap);
                  label.Text = result.Text;
        } catch (Exception ex) {
          label.Text = ex.ToString();
          Console.WriteLine(ex.ToString());
        }

        return true;
      }

      // This method is required in iPhoneOS 3.0
      public override void OnActivated (UIApplication application)
      {
      }
    }
  }

## zxing
ZXing is released under the Apache 2.0 license.
ZXing can be found here: http://code.google.com/p/zxing/
A copy of the Apache 2.0 license can be found here: http://www.apache.org/licenses/LICENSE-2.0

## System.Drawing
The System.Drawing classes included are from the mono source code which is property of Novell.
Copyright notice is intact in source code files.
