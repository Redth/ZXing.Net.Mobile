# ZXing.Net.Mobile

![ZXing.Net.Mobile Logo](https://raw.github.com/Redth/ZXing.Net.Mobile/master/Icon_128x128.png)

ZXing.Mobile is a C#/.NET library based on the open source Barcode Library: ZXing (Zebra Crossing), using the ZXing.Net Port.  It works with MonoTouch, Mono for Android, and Windows Phone.  The goal of ZXing.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications.  

*NOTE*: ZXing.Mobile is still quite BETA!  Your mileage may vary!

### Usage
The simplest example of using ZXing.Mobile looks something like this:

```csharp  
  var scanner = new ZXing.Mobile.MobileBarcodeScanner();
  scanner.Scan().ContinueWith((result) => {   
     if (result != null)
       Console.WriteLine("Scanned Barcode: " + result.Text);
  });
```

###Features
- MonoTouch
- Mono for Android
- Windows Phone
- Simple API - Scan in as little as 2 lines of code!

###Thanks
ZXing.Mobile is a combination of a lot of peoples' work that I've put together (including my own).  So naturally, I'd like to thank everyone who's helped out in any way.  Those of you I know have helped I'm listing here, but anyone else that was involved, please let me know!

- ZXing Project and those responsible for porting it to C#
- John Carruthers - https://github.com/JohnACarruthers/zxing.MonoTouch
- Martin Bowling - https://github.com/martinbowling
- Alex Corrado - https://github.com/chkn/zxing.MonoTouch
- ZXing.Net Project - http://zxingnet.codeplex.com - HUGE effort here to port ZXing to .NET

###Custom Overlays
By default, ZXing.Mobile provides a very simple overlay for your barcode scanning interface.  This overlay consists of a horizontal red line centered in the scanning 'window' and semi-transparent borders on the top and bottom of the non-scanning area.  You also have the opportunity to customize the top and bottom text that appears in this overlay.

If you want to customize the overlay, you must create your own View for each platform.  You can customize your overlay like this:

```csharp
var scanner = new ZXing.Mobile.MobileBarcodeScanner();
scanner.UseCustomOverlay = true;
scanner.CustomOverlay = myCustomOverlayInstance;
scanner.Scan().ContinueWith((result) => { //Handle Result });
```

Keep in mind that when using a Custom Overlay, you are responsible for the entire overlay (you cannot mix and match custom elements with the default overlay).  The *ZxingScanner* instance has a *CustomOverlay* property, however on each platform this property is of a different type:

- MonoTouch => **UIView**
- Mono for Android => **View**
- Windows Phone => **UIElement**

All of the platform samples have examples of custom overlays.

###Barcode Formats
By default, all barcode formats are monitored while scanning.  You can change which formats to check for by passing a ZxingScanningOptions instance into the StartScanning method:

```csharp
var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
options.PossibleFormats = new List<ZXing.BarcodeFormat>() { 
    ZXing.BarcodeFormat.Ean8 | ZXing.BarcodeFormat.Ean13 };

var scanner = new ZXing.Mobile.MobileBarcodeScanner();
scanner.Scan(options).ContinueWith((result) => { //Handle results });
````

###Samples
Samples for implementing ZXing.Mobile can be found in the /*sample*/ folder.  There is a sample for each platform including examples of how to use custom overlays.



###License
Apache ZXing.Mobile Copyright 2012 The Apache Software Foundation
This product includes software developed at The Apache Software Foundation (http://www.apache.org/).

### ZXing.Net
ZXing.Net is released under the Apache 2.0 license.
ZXing.Net can be found here: http://code.google.com/p/zxing/
A copy of the Apache 2.0 license can be found here: http://www.apache.org/licenses/LICENSE-2.0

### ZXing
ZXing is released under the Apache 2.0 license.
ZXing can be found here: http://code.google.com/p/zxing/
A copy of the Apache 2.0 license can be found here: http://www.apache.org/licenses/LICENSE-2.0

### System.Drawing
The System.Drawing classes included are from the mono source code which is property of Novell.
Copyright notice is intact in source code files.
