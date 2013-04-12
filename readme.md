# ZXing.Net.Mobile

![ZXing.Net.Mobile Logo](https://raw.github.com/Redth/ZXing.Net.Mobile/master/zxing.net.mobile_128x128.png)

ZXing.Net.Mobile is a C#/.NET library based on the open source Barcode Library: ZXing (Zebra Crossing), using the ZXing.Net Port.  It works with Xamarin.iOS, Xamarin.Android, and Windows Phone.  The goal of ZXing.Net.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications.  

*NOTE*: ZXing.Net.Mobile is still quite BETA!  Your mileage may vary!

### Usage
The simplest example of using ZXing.Net.Mobile looks something like this:

```csharp  
buttonScan.Click += (sender, e) => {

	var scanner = new ZXing.Mobile.MobileBarcodeScanner();
	scanner.Scan().ContinueWith(t => {   
   		if (t.Result != null)
    		Console.WriteLine("Scanned Barcode: " + t.Result.Text);
	});

};
```

###Features
- Xamarin.iOS
- Xamarin.Android
- Windows Phone
- Simple API - Scan in as little as 2 lines of code!

###Changes
 - v1.3.4
   - iOS: Scanning Engine rebuilt to use AVCaptureSession
   - iOS: ZXingScannerView inherits from UIView can now be used independently for advanced use cases
   - Android: Fixed Torch bug on Android
   - Android: Front Cameras now work in Sample by default
   - Performance improvements

   
 - v1.3.3
   - Fixed Android not scanning some barcodes in Portrait
   - Fixed Android scanning very slowly
   - Added to MobileBarcodeScanningOptions: IntervalBetweenAnalyzingFrames to configure how 'fast' frames from the live scanner view are analyzed in an attempt to decode barcodes 
   
###Thanks
ZXing.Net.Mobile is a combination of a lot of peoples' work that I've put together (including my own).  So naturally, I'd like to thank everyone who's helped out in any way.  Those of you I know have helped I'm listing here, but anyone else that was involved, please let me know!

- ZXing Project and those responsible for porting it to C#
- John Carruthers - https://github.com/JohnACarruthers/zxing.MonoTouch
- Martin Bowling - https://github.com/martinbowling
- Alex Corrado - https://github.com/chkn/zxing.MonoTouch
- ZXing.Net Project - http://zxingnet.codeplex.com - HUGE effort here to port ZXing to .NET

###Custom Overlays
By default, ZXing.Net.Mobile provides a very simple overlay for your barcode scanning interface.  This overlay consists of a horizontal red line centered in the scanning 'window' and semi-transparent borders on the top and bottom of the non-scanning area.  You also have the opportunity to customize the top and bottom text that appears in this overlay.

If you want to customize the overlay, you must create your own View for each platform.  You can customize your overlay like this:

```csharp
var scanner = new ZXing.Mobile.MobileBarcodeScanner();
scanner.UseCustomOverlay = true;
scanner.CustomOverlay = myCustomOverlayInstance;
scanner.Scan().ContinueWith(t => { //Handle Result });
```

Keep in mind that when using a Custom Overlay, you are responsible for the entire overlay (you cannot mix and match custom elements with the default overlay).  The *ZxingScanner* instance has a *CustomOverlay* property, however on each platform this property is of a different type:

- Xamarin.iOS => **UIView**
- Xamarin.Android => **View**
- Windows Phone => **UIElement**

All of the platform samples have examples of custom overlays.

###Barcode Formats
By default, all barcode formats are monitored while scanning.  You can change which formats to check for by passing a ZxingScanningOptions instance into the StartScanning method:

```csharp
var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
options.PossibleFormats = new List<ZXing.BarcodeFormat>() { 
    ZXing.BarcodeFormat.Ean8, ZXing.BarcodeFormat.Ean13 
};

var scanner = new ZXing.Mobile.MobileBarcodeScanner();
scanner.Scan(options).ContinueWith(t => { //Handle results });
````

###Samples
Samples for implementing ZXing.Net.Mobile can be found in the /*sample*/ folder.  There is a sample for each platform including examples of how to use custom overlays.



###License
Apache ZXing.Net.Mobile Copyright 2012 The Apache Software Foundation
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
