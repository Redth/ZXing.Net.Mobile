# Getting Started #

You can use ZXing.Net.Mobile in your Xamarin.iOS, Xamarin.Android, and Windows Phone apps.  Simply download the component, and reference the dll's for yoru platform.

### Usage
The simplest example of using ZXing.Net.Mobile looks something like this:

```csharp  
buttonScan.Click += (sender, e) => {
	
	//NOTE: On Android you MUST pass a Context into the Constructor!
	var scanner = new ZXing.Mobile.MobileBarcodeScanner();
	var result = await scanner.Scan();

	if (result != null)
    	Console.WriteLine("Scanned Barcode: " + result.Text);
};
```

### Android Versions
The component should work on Android 2.2 or higher.  In Xamarin.Android there are 3 places in the project settings relating to Android version.  YOU ***MUST*** set the Project Options -> Build -> General -> Target Framework to ***2.3*** or higher.  If you still want to use 2.2, you can set the Project Options -> Build -> Android Application -> Minimum Android version to 2.2, but be sure to set the Target Android version in this section to 2.3 or higher.

You must also add a reference to the Component `Android Support Library v4` https://components.xamarin.com/view/xamandroidsupportv4-18 from the Component Store.

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
 - iOS: AVCaptureScannerView (UIView) - This is API equivalent to ZXingScannerView, but uses Apple's AVCaptureSession Metadata engine to scan the barcodes instead of ZXing.Net.  See AVCaptureScannerViewController.cs View Controller for an example of how to use this view
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

###License
Apache ZXing.Net.Mobile Copyright 2012 The Apache Software Foundation
This product includes software developed at The Apache Software Foundation (http://www.apache.org/).

### ZXing.Net
ZXing.Net is released under the Apache 2.0 license.
ZXing.Net can be found here: http://zxingnet.codeplex.com/
A copy of the Apache 2.0 license can be found here: http://www.apache.org/licenses/LICENSE-2.0

### ZXing
ZXing is released under the Apache 2.0 license.
ZXing can be found here: https://github.com/zxing/zxing
A copy of the Apache 2.0 license can be found here: http://www.apache.org/licenses/LICENSE-2.0

### System.Drawing
The System.Drawing classes included are from the mono source code which is property of Novell.
Copyright notice is intact in source code files.
