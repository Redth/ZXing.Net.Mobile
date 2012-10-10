# ZxingSharp.Mobile

ZxingSharp.Mobile is a C#/.NET library based on the open source Barcode Library: ZXing (Zebra Crossing).  It works with MonoTouch, Mono for Android, and Windows Phone.  The goal of ZxingSharp.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications.  

*NOTE*: ZxingSharp.Mobile is still quite BETA!  Your mileage may vary!

### Usage
The simplest example of using ZxingSharp.Mobile looks something like this:

```csharp  
  var scanner = new ZxingSharp.Mobile.ZxingScanner();
  scanner.StartScanning((result) => {   
     if (result != null)
       Console.WriteLine("Scanned Barcode: " + result.Value);
  });
```

###Features
- MonoTouch
- Mono for Android
- Windows Phone
- Simple API - Scan in as little as 2 lines of code!


###Custom Overlays
By default, ZxingSharp.Mobile provides a very simple overlay for your barcode scanning interface.  This overlay consists of a horizontal red line centered in the scanning 'window' and semi-transparent borders on the top and bottom of the non-scanning area.  You also have the opportunity to customize the top and bottom text that appears in this overlay.

If you want to customize the overlay, you must create your own View for each platform.  You can customize your overlay like this:

```csharp
var scanner = new ZxingSharp.Mobile.ZxingScanner();
scanner.UseCustomOverlay = true;
scanner.CustomOverlay = myCustomOverlayInstance;
scanner.StartScanning((result) => { //Handle Result });
```

Keep in mind that when using a Custom Overlay, you are responsible for the entire overlay (you cannot mix and match custom elements with the default overlay).  The *ZxingScanner* instance has a *CustomOverlay* property, however on each platform this property is of a different type:

- MonoTouch => **UIView**
- Mono for Android => **View**
- Windows Phone => **UIElement**

All of the platform samples have examples of custom overlays.

###Barcode Formats
By default, all barcode formats are monitored while scanning.  You can change which formats to check for by passing a ZxingScanningOptions instance into the StartScanning method:

```csharp
var options = new ZxingSharp.Mobile.ZxingScanningOptions();
options.BarcodeFormats = ZxingSharp.Mobile.ZxingBarcodeFormat.Ean8 |
                         ZxingSharp.Mobile.ZxingBarcodeFormat.Ean13;

var scanner = new ZxingSharp.Mobile.ZxingScanner();
scanner.StartScanning(options, (result) => { //Handle results });
````

###Samples
Samples for implementing ZxingSharp.Mobile can be found in the /*sample*/ folder.  There is a sample for each platform including examples of how to use custom overlays.



###License
Apache ZxingSharp.Mobile Copyright 2012 The Apache Software Foundation
This product includes software developed at The Apache Software Foundation (http://www.apache.org/).

### ZXing
ZXing is released under the Apache 2.0 license.
ZXing can be found here: http://code.google.com/p/zxing/
A copy of the Apache 2.0 license can be found here: http://www.apache.org/licenses/LICENSE-2.0

### System.Drawing
The System.Drawing classes included are from the mono source code which is property of Novell.
Copyright notice is intact in source code files.
