ZXing.Net.Mobile is a C#/.NET library based on the open source Barcode Library: ZXing (Zebra Crossing), using the ZXing.Net Port. It works with Xamarin.iOS, Xamarin.Android, and Windows Phone. The goal of ZXing.Net.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications!

GitHub Project: https://github.com/Redth/ZXing.Net.Mobile

### Usage
```csharp
buttonScan.Click += (sender, e) => {

  //NOTE: On Android, you MUST pass a Context into the Constructor!
	var scanner = new ZXing.Mobile.MobileBarcodeScanner();
	var result = await scanner.Scan();
  
  if (result != null)
    Console.WriteLine("Scanned Barcode: " + result.Text);
};
```


###Features
- Xamarin.iOS
- Xamarin.Android
- Windows Phone
- Simple API - Scan in as little as 2 lines of code!
- Scanner as a View - UIView (iOS) / Fragment (Android) / Control (WP)


###Custom Overlays
By default, ZXing.Net.Mobile provides a very simple overlay for your barcode scanning interface.  This overlay consists of a horizontal red line centered in the scanning 'window' and semi-transparent borders on the top and bottom of the non-scanning area.  You also have the opportunity to customize the top and bottom text that appears in this overlay.

If you want to customize the overlay, you must create your own View for each platform.  You can customize your overlay like this:

```csharp
var scanner = new ZXing.Mobile.MobileBarcodeScanner();
scanner.UseCustomOverlay = true;
scanner.CustomOverlay = myCustomOverlayInstance;
var result = await scanner.Scan();
//Handle result
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
var result = await scanner.Scan(options);
//Handle result
```

###Using the ZXingScanner View / Fragment / Control
On each platform, the ZXing scanner has been implemented as a reusable component (view, fragment, or control), and it is possible to use the reusable component directly without using the MobileBarcodeScanner class at all.  On each platform, the instance of the view/fragment/control contains the necessary properties and methods required to control your scanner.  By default, the default overlay is automatically used, unless you set the CustomOverlay property as well as the UseCustomOverlay property on the instance of the view/fragment/control.  You can use methods such as ToggleTorch() or StopScanning() on the view/fragment/control, however you are responsible for calling StartScanning(...) with a callback and an instance of MobileBarcodeScanningOptions when you are ready for the view's scanning to begin.  You are also responsible for stopping scanning if you want to cancel at any point.

The view/fragment/control classes for each platform are:

 - iOS: ZXingScannerView (UIView) - See ZXingScannerViewController.cs View Controller for an example of how to use this view
 - Android: ZXingScannerFragment (Fragment) - See ZXingActivity.cs Activity for an example of how to use this fragment
 - Windows Phone: ZXingScannerControl (UserControl) - See ScanPage.xaml Page for an example of how to use this Control


###Using Apple's AVCaptureSession (iOS7 Built in) Barcode Scanning
In iOS7, Apple added some API's to allow for scanning of barcodes in an AVCaptureSession.  The latest version of ZXing.Net.Mobile gives you the option of using this instead of the ZXing scanning engine.  You can use the `AVCaptureScannerView` or the `AVCaptureScannerViewController` classes directly just the same as you would use their ZXing* equivalents.  Or, in your `MobileBarcodeScanner`, there is now an overload to use the AV Capture Engine:

```csharp
//Scan(MobileBarcodeScanningOptions options, bool useAVCaptureEngine)
scanner.Scan(options, true);
```
In the MobileBarcodeScanner, even if you specify to use the AVCaptureSession scanning, it will gracefully degrade to using ZXing if the device doesn't support this (eg: if it's not iOS7 or newer), or if you specify a barcode format in your scanning options which the AVCaptureSession does not support for detection.  The AVCaptureSession can only decode the following barcodes:

- Aztec
- Code 128
- Code 39
- Code 93
- EAN13
- EAN8
- PDF417
- QR
- UPC-E

