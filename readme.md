# ZXing.Net.Mobile

[![Join the chat at https://gitter.im/Redth/ZXing.Net.Mobile](https://badges.gitter.im/Redth/ZXing.Net.Mobile.svg)](https://gitter.im/Redth/ZXing.Net.Mobile?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

![ZXing.Net.Mobile Logo](https://raw.github.com/Redth/ZXing.Net.Mobile/master/zxing.net.mobile_128x128.png)

ZXing.Net.Mobile is a C#/.NET library based on the open source Barcode Library: [ZXing (Zebra Crossing)](https://github.com/zxing/zxing), using the [ZXing.Net Port](https://github.com/micjahn/ZXing.Net).  It works with Xamarin.iOS, Xamarin.Android, Tizen, and UWP.  The goal of ZXing.Net.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications.

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2Fredth%2FZXing.Net.Mobile%2Fbadge&style=flat)](https://actions-badge.atrox.dev/redth/ZXing.Net.Mobile/goto)
[![NuGet](https://img.shields.io/nuget/v/ZXing.Net.Mobile.svg)](https://www.nuget.org/packages/ZXing.Net.Mobile/)
[![NuGet](https://img.shields.io/nuget/dt/ZXing.Net.Mobile.svg)](https://www.nuget.org/packages/ZXing.Net.Mobile/)

### Usage
The simplest example of using ZXing.Net.Mobile looks something like this:

```csharp  
buttonScan.Click += (sender, e) => {

	#if __ANDROID__
	// Initialize the scanner first so it can track the current context
	MobileBarcodeScanner.Initialize (Application);
  	#endif
  	
	var scanner = new ZXing.Mobile.MobileBarcodeScanner();

	var result = await scanner.Scan();

	if (result != null)
		Console.WriteLine("Scanned Barcode: " + result.Text);
};
```


#### Xamarin Forms
For Xamarin Forms there is a bit more setup needed.  You will need to initialize the library on each platform in your platform specific app project.


##### Android 

On Android, in your main `Activity`'s `OnCreate (..)` implementation, call:

```csharp
Xamarin.Essentials.Platform.Init(Application);
ZXing.Net.Mobile.Forms.Android.Platform.Init();
```

ZXing.Net.Mobile for Xamarin.Forms also handles the new Android permission request model for you via Xamarin.Essentials, but you will need to add the following override implementation to your main `Activity` as well:

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

The `Camera` permission should be automatically included for you in the `AndroidManifest.xml` however if you would like to use the [Flashlight API](https://developer.android.com/about/versions/marshmallow/android-6.0.html#flashlight) you will still need to add the `Flashlight` permission yourself.  You can do this by using the following assembly level attribute:

```csharp
[assembly: UsesPermission (Android.Manifest.Permission.Flashlight)]
```

##### iOS

In your `AppDelegate`'s `FinishedLaunching (..)` implementation, call:

```csharp
ZXing.Net.Mobile.Forms.iOS.Platform.Init();
```


##### Windows Universal UWP

In your main `Page`'s constructor, you should add:

```csharp
ZXing.Net.Mobile.Forms.WindowsUniversal.Platform.Init();
```

If you notice that finishing scanning or pressing the back button is causing your Page to jump back further than you'd like, or if you're having trouble updating the UI of a Page after scanning is completed, you may need to set `NavigationCacheMode="Enabled"` within your Page's XAML `<Page ... />` element.


##### macOS

In your `AppDelegate`'s `FinishedLaunching (..)` implementation, call:

```csharp
ZXing.Net.Mobile.Forms.MacOS.Platform.Init();
```


### Features
- Xamarin.iOS
- Xamarin.Android
- Tizen
- UWP
- Xamarin.Mac (rendering only, not scanning)
- Simple API - Scan in as little as 2 lines of code!
- Scanner as a View - UIView (iOS) / Fragment (Android) / Control (WP)

### Custom Overlays
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
- UWP => **UIElement**

All of the platform samples have examples of custom overlays.

### Barcode Formats
By default, all barcode formats are monitored while scanning.  You can change which formats to check for by passing a ZxingScanningOptions instance into the StartScanning method:

```csharp
//NOTE: On Android you should call the initialize method with an application instance
#if __ANDROID__
// Initialize the scanner first so it can track the current context
MobileBarcodeScanner.Initialize (Application);
#endif

var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
options.PossibleFormats = new List<ZXing.BarcodeFormat>() { 
    ZXing.BarcodeFormat.Ean8, ZXing.BarcodeFormat.Ean13 
};

var scanner = new ZXing.Mobile.MobileBarcodeScanner(); 
var result = await scanner.Scan(options);
//Handle result
```

### Samples
There is a sample for each platform including examples of how to use custom overlays.

### Using the ZXingScanner View / Fragment / Control
On each platform, the ZXing scanner has been implemented as a reusable component (view, fragment, or control), and it is possible to use the reusable component directly without using the MobileBarcodeScanner class at all.  On each platform, the instance of the view/fragment/control contains the necessary properties and methods required to control your scanner.  By default, the default overlay is automatically used, unless you set the CustomOverlay property as well as the UseCustomOverlay property on the instance of the view/fragment/control.  You can use methods such as ToggleTorch() or StopScanning() on the view/fragment/control, however you are responsible for calling StartScanning(...) with a callback and an instance of MobileBarcodeScanningOptions when you are ready for the view's scanning to begin.  You are also responsible for stopping scanning if you want to cancel at any point.

The view/fragment/control classes for each platform are:

 - iOS: ZXingScannerView (UIView) - See ZXingScannerViewController.cs View Controller for an example of how to use this view
 - iOS: AVCaptureScannerView (UIView) - This is API equivalent to ZXingScannerView, but uses Apple's AVCaptureSession Metadata engine to scan the barcodes instead of ZXing.Net.  See AVCaptureScannerViewController.cs View Controller for an example of how to use this view
 - Android: ZXingScannerFragment (Fragment) - See ZXingActivity.cs Activity for an example of how to use this fragment
 - UWP: ZXingScannerControl (UserControl) - See ScanPage.xaml Page for an example of how to use this Control

### Using Apple's AVCaptureSession (iOS7 Built in) Barcode Scanning
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


### Thanks
ZXing.Net.Mobile is a combination of a lot of peoples' work that I've put together (including my own).  So naturally, I'd like to thank everyone who's helped out in any way.  Those of you I know have helped I'm listing here, but anyone else that was involved, please let me know!

- ZXing Project and those responsible for porting it to C#
- John Carruthers - https://github.com/JohnACarruthers/zxing.MonoTouch
- Martin Bowling - https://github.com/martinbowling
- Alex Corrado - https://github.com/chkn/zxing.MonoTouch
- ZXing.Net Project - https://github.com/micjahn/ZXing.Net - HUGE effort here to port ZXing to .NET



### License
Apache ZXing.Net.Mobile Copyright 2012 The Apache Software Foundation
This product includes software developed at The Apache Software Foundation (http://www.apache.org/).

### ZXing.Net
ZXing.Net is released under the Apache 2.0 license.
ZXing.Net can be found here: https://github.com/micjahn/ZXing.Net
A copy of the Apache 2.0 license can be found here: https://github.com/micjahn/ZXing.Net/blob/master/COPYING

### ZXing
ZXing is released under the Apache 2.0 license.
ZXing can be found here: http://code.google.com/p/zxing/
A copy of the Apache 2.0 license can be found here: https://github.com/zxing/zxing/blob/master/LICENSE

### System.Drawing
The System.Drawing classes included are from the mono source code which is property of Novell.
Copyright notice is intact in source code files.
