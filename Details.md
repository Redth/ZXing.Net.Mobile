ZXing.Net.Mobile is a C#/.NET library based on the open source Barcode Library: ZXing (Zebra Crossing), using the ZXing.Net Port. It works with Xamarin.iOS, Xamarin.Android, and Windows Phone. The goal of ZXing.Net.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications!

GitHub Project: https://github.com/Redth/ZXing.Net.Mobile

### Changes
 - v1.3.3
   - Fixed Android not scanning some barcodes in Portrait
   - Fixed Android scanning very slowly
   - Added to MobileBarcodeScanningOptions: IntervalBetweenAnalyzingFrames to configure how 'fast' frames from the live scanner view are analyzed in an attempt to decode barcodes 


### Usage
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
