ZXing.Net.Mobile is a C#/.NET library based on the open source Barcode Library: ZXing (Zebra Crossing), using the ZXing.Net Port. It works with Xamarin.iOS, Xamarin.Android, and Windows Phone. The goal of ZXing.Net.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications!

GitHub Project: https://github.com/Redth/ZXing.Net.Mobile

### Changes
 - v1.3.5
   - Views for each Platform - Encapsulates scanner functionality in a reusable view
    - iOS: ZXingScannerView as a UIView
    - Android: ZXingScannerFragment as a Fragment
    - Windows Phone: ZXingScannerControl as a UserControl
   - Scanning logic improvements from ZXing.Net project
   - Compiled against Xamarin Stable channel
   - Performance improvements
   - Bug fixes

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


### Usage
```csharp
buttonScan.Click += (sender, e) => {

  //NOTE: On Android, you MUST pass a Context into the Constructor!
	var scanner = new ZXing.Mobile.MobileBarcodeScanner();
	scanner.Scan().ContinueWith(t => {   
   		if (t.Result != null)
    		Console.WriteLine("Scanned Barcode: " + t.Result.Text);
	});

};
```

### Alpha / Beta Channels
Please note that this component was built against the **Stable Channel**.  In the past the component was compiled for the Alpha / Beta channels.  If you are working in the Alpha or Beta channel, you may need to go to the GitHub Repository and download the source and compile it manually for the Alpha or Beta.

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

###Using the ZXingScanner View / Fragment / Control
On each platform, the ZXing scanner has been implemented as a reusable component (view, fragment, or control), and it is possible to use the reusable component directly without using the MobileBarcodeScanner class at all.  On each platform, the instance of the view/fragment/control contains the necessary properties and methods required to control your scanner.  By default, the default overlay is automatically used, unless you set the CustomOverlay property as well as the UseCustomOverlay property on the instance of the view/fragment/control.  You can use methods such as ToggleTorch() or StopScanning() on the view/fragment/control, however you are responsible for calling StartScanning(...) with a callback and an instance of MobileBarcodeScanningOptions when you are ready for the view's scanning to begin.  You are also responsible for stopping scanning if you want to cancel at any point.

The view/fragment/control classes for each platform are:

 - iOS: ZXingScannerView (UIView) - See ZXingScannerViewController.cs View Controller for an example of how to use this view
 - Android: ZXingScannerFragment (Fragment) - See ZXingActivity.cs Activity for an example of how to use this fragment
 - Windows Phone: ZXingScannerControl (UserControl) - See ScanPage.xaml Page for an example of how to use this Control
