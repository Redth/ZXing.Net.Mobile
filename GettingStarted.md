# Getting Started #

You can use ZXing.Net.Mobile in your Xamarin.iOS, Xamarin.Android, and Windows Phone apps.  Simply download the component, and reference the dll's for yoru platform.

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
```

###Important Additional Notes

- Scanning will crash the iOS Simulator.  Currently scanning only works on devices in Xamarin.iOS


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
