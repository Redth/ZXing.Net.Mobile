# Getting Started

ZXing.Net.Mobile for Forms is meant to be used in your Xamarin.Forms apps.  It comes with Forms controls and pages for scanning and displaying barcodes.

If you are looking for barcode scanning functionality in your non-Forms Xamarin / Windows apps, check out the original [ZXing.Net.Mobile Component](https://components.xamarin.com/view/zxing.net.mobile).

# Usage

The easiest way to use scanner is to create an instance of the `ZXingScannerPage`: 

```csharp
var scanPage = new ZXingScannerPage ();

scanPage.OnScanResult += (result) => {
	// Stop scanning
    scanPage.IsScanning = false;

	// Pop the page and show the result
    Device.BeginInvokeOnMainThread (() => {
        Navigation.PopAsync ();        
        DisplayAlert("Scanned Barcode", result.Text, "OK");
    });
};

// Navigate to our scanner page
await Navigation.PushAsync (scanPage);
```


## Additional Setup Required

For each platform there is some additional setup required:

### Android 

On Android, in your main `Activity`'s `OnCreate (..)` implementation, call:

```csharp
ZXing.Net.Mobile.Forms.Android.Platform.Init();
```

ZXing.Net.Mobile for Xamarin.Forms also handles the new Android permission request model for you, but you will need to add the following override implementation to your main `Activity` as well:

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    global::ZXing.Net.Mobile.Forms.Android.PermissionsHandler.OnRequestPermissionsResult (requestCode, permissions, grantResults);           
}
```

The `Camera` permission should be automatically included for you in the `AndroidManifest.xml` however if you would like to use the Torch API's you will still need to add the `Flashlight` permission yourself.  You can do this by using the following assembly level attribute:

```csharp
[assembly: UsesPermission (Android.Manifest.Permission.Flashlight)]
```

### iOS

In your `AppDelegate`'s `FinishedLaunching (..)` implementation, call:

```csharp
ZXing.Net.Mobile.Forms.Android.Platform.Init();
```


### Windows Phone
In your main `Page`'s constructor, you should add:

```csharp
ZXing.Net.Mobile.Forms.WindowsPhone.ZXingScannerViewRenderer.Init();
```

### Windows Universal UWP

In your main `Page`'s constructor, you should add:

```csharp
ZXing.Net.Mobile.Forms.WindowsUniversal.ZXingScannerViewRenderer.Init();
```



# Scanning in a View

If you need more customization, or do not want the scanner to take up its own Page, you can also use the `ZXingScannerView` in your own custom page:

```csharp
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;

namespace FormsSample
{
    public class CustomScanPage : ContentPage
    {
        ZXingScannerView zxing;
        ZXingDefaultOverlay overlay;

        public CustomScanPage () : base ()
        {
            zxing = new ZXingScannerView
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };
            zxing.OnScanResult += (result) => 
                Device.BeginInvokeOnMainThread (async () => {

                    // Stop analysis until we navigate away so we don't keep reading barcodes
                    zxing.IsAnalyzing = false;

                    // Show an alert
                    await DisplayAlert ("Scanned Barcode", result.Text, "OK");

                    // Navigate away
                    await Navigation.PopAsync ();
                });

            overlay = new ZXingDefaultOverlay
            {
                TopText = "Hold your phone up to the barcode",
                BottomText = "Scanning will happen automatically",
                ShowFlashButton = zxing.HasTorch,
            };
            overlay.FlashButtonClicked += (sender, e) => {
                zxing.IsTorchOn = !zxing.IsTorchOn;
            };
            var grid = new Grid
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
            };
            grid.Children.Add(zxing);
            grid.Children.Add(overlay);

            // The root page of your application
            Content = grid;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            zxing.IsScanning = true;
        }

        protected override void OnDisappearing()
        {
            zxing.IsScanning = false;

            base.OnDisappearing();
        }
    }
}
```


##Custom Overlays

It's very simple to use a custom overlay.  If you are using the `ZXingScannerPage` you can pass in any `View` for the `customOverlay` parameter in the constructor.

```csharp
// Create our custom overlay
var customOverlay = new StackLayout {
    HorizontalOptions = LayoutOptions.FillAndExpand,
    VerticalOptions = LayoutOptions.FillAndExpand
};
var torch = new Button {
    Text = "Toggle Torch"
};
torch.Clicked += delegate {
    scanPage.ToggleTorch ();
};
customOverlay.Children.Add (torch);

// Pass in the custom overlay
scanPage = new ZXingScannerPage (customOverlay: customOverlay);
scanPage.OnScanResult += (result) => {
    scanPage.IsScanning = false;

    Device.BeginInvokeOnMainThread(() =>
    {
        Navigation.PopAsync();
        DisplayAlert("Scanned Barcode", result.Text, "OK");
    });
};
await Navigation.PushAsync (scanPage);
```

If you are using `ZXingScannerView` on your own page, of course you are responsible for adding your own overlay.


## Barcode Formats

By default, all barcode formats are monitored while scanning.  You can change which formats to check for by passing a `MobileBarcodeScanningOptions` instance into the `ZXingScannerPage`'s constructor.

```csharp
var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
options.PossibleFormats = new List<ZXing.BarcodeFormat>() { 
  ZXing.BarcodeFormat.Ean8, ZXing.BarcodeFormat.Ean13 
};

var scanPage = new ZXingScannerPage (options);

// ...
```

You can also set the `Options` property if you are using the `ZXingScannerView` control directly.


## Displaying / Generating Barcodes

There is also a `ZXingBarcodeImageView` control available that can be used within any Xamarin.Forms page:

```csharp
public class BarcodePage : ContentPage
{
    ZXingBarcodeImageView barcode;

    public BarcodePage ()
    {
        barcode = new ZXingBarcodeImageView {
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,                   
        };
        barcode.BarcodeFormat = ZXing.BarcodeFormat.QR_CODE;
        barcode.BarcodeOptions.Width = 300;
        barcode.BarcodeOptions.Height = 300;
        barcode.BarcodeOptions.Margin = 10;
        barcode.BarcodeValue = "ZXing.Net.Mobile";

        Content = barcode;
    }
}
```
