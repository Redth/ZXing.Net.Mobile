ZXing.Mobile is a C#/.NET library based on the open source Barcode Library: ZXing (Zebra Crossing), using the ZXing.Net Port. It works with MonoTouch, Mono for Android, and Windows Phone. The goal of ZXing.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications!

GitHub Project: https://github.org/Redth/ZXing.Net.Mobile

### Usage
[code:csharp]

	var scanner = new ZXing.Mobile.MobileBarcodeScanner();
  scanner.Scan().ContinueWith((result) => {   
     if (result != null)
       Console.WriteLine("Scanned Barcode: " + result.Text);
  });

[code]

###Features
- MonoTouch
- Mono for Android
- Windows Phone
- Simple API - Scan in as little as 2 lines of code!


###Custom Overlays
By default, ZXing.Mobile provides a very simple overlay for your barcode scanning interface.  This overlay consists of a horizontal red line centered in the scanning 'window' and semi-transparent borders on the top and bottom of the non-scanning area.  You also have the opportunity to customize the top and bottom text that appears in this overlay.

If you want to customize the overlay, you must create your own View for each platform.  You can customize your overlay like this:

[code:csharp]
  var scanner = new ZXing.Mobile.MobileBarcodeScanner();
  scanner.UseCustomOverlay = true;
  scanner.CustomOverlay = myCustomOverlayInstance;
  scanner.Scan().ContinueWith((result) => { //Handle Result });
[code]

Keep in mind that when using a Custom Overlay, you are responsible for the entire overlay (you cannot mix and match custom elements with the default overlay).  The *ZxingScanner* instance has a *CustomOverlay* property, however on each platform this property is of a different type:

- MonoTouch => **UIView**
- Mono for Android => **View**
- Windows Phone => **UIElement**

All of the platform samples have examples of custom overlays.

###Barcode Formats
By default, all barcode formats are monitored while scanning.  You can change which formats to check for by passing a ZxingScanningOptions instance into the StartScanning method:

[code:csharp]
  var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
  options.PossibleFormats = new List<ZXing.BarcodeFormat>() { 
    ZXing.BarcodeFormat.Ean8 | ZXing.BarcodeFormat.Ean13 };

  var scanner = new ZXing.Mobile.MobileBarcodeScanner();
  scanner.Scan(options).ContinueWith((result) => { //Handle results });
[code]
