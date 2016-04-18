# Changelog

 - v1.4.7.1
        - iOS: Updated Unified support

 - v1.4.7
        - Updated ZXing.Net
        - iOS: Better destruction of default overlay to prevent memory leak
        - WP8: Fixed issue with HW shutter pressed after camera no longer available
        - Android: Fixed `Scan` method

 - v1.4.6
        - Android: Updated Android Support Library v4 component used (20.0.0.3)
        - Updated ZXing.Net (SVN commit 88850)
        - Android: now takes `UseFrontCameraIfAvailable` into account ([#120](https://github.com/Redth/ZXing.Net.Mobile/issues/120))
        - Added camera resolution selector to MobileBarcodeScanningOptions
        - WP8: Fixed a null reference exception ([#104](https://github.com/Redth/ZXing.Net.Mobile/issues/104))

 - v1.4.5
        - Android: Updated Android Support Library v4 component used (20.0.0)
        - Updated ZXing.Net
        - WP8: Fixed an issue with an exception on subsequent scans ([#102](https://github.com/Redth/ZXing.Net.Mobile/pull/102))
        - Android: Updated Android Support Library v4 component used (4.19.0.1)

 - v1.4.4
        - iOS: Fixed issue with loading view size in landscape orientation ([#100](https://github.com/Redth/ZXing.Net.Mobile/issues/100))
        - Android: Fixed issue with scanning not working from timestamps ([#98](https://github.com/Redth/ZXing.Net.Mobile/issues/98))

 - v1.4.3
        - iOS: Fixed slowness after 4-5 scans ([#71](https://github.com/Redth/ZXing.Net.Mobile/issues/71))
        - WP8: Fixed preview sometimes not starting
        - Android: Fixed resuming fragment which closes
        - Android: Scanner rotates as needed for orientation
        - Android: Removed YuvImage use for better, faster decoding with less memory usage
        - Android: Added permission checks
        - Updated ZXing.Net version used

 - v1.4.2
 	- WP8: Fixed crash when pressing back while camera initializes
 	- Android: Added merged workaround from @chrisntr support for Google Glass	
 	- Android: Now using the ***Android Support Library v4*** from the component store
 	
 - v1.4.1
 	- iOS: Fixed multiple scanner launches causing Scanning to no longer work
 	- Android: Fixed rotation on some tablets showing incorrectly
 	
 - v1.4.0
   - iOS: Added iOS7's built in AVCaptureSession MetadataObject barcode scanning as an option
   - iOS: Fixed Offset of overlay and preview layers when a non-zero based offset was specified
   - iOS: Added code to remove session inputs/outputs to improve performance between scans
   - iOS: Front Camera now possible on iOS
   - Android: Fixed rotation
   - Windows Phone: Added Windows Phone 8 samples and builds
   - Windows Phone: Dropped explicit support for WP7x (code is still there, but no binaries shipped)
   - Updated ZXing.NET version used
   - General performance enhancements and bug fixes
   
 - v1.3.6
   - Built for Xamarin 3.0 with async/await support
   - iOS: Added PauseScanning and ResumeScanning options
   - iOS: Added empty ctor to ZXingScannerView

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
