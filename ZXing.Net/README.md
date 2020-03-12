# ZXing.Net

ZXing.Net is a .Net port of the original java-based barcode reader and generator library zxing written by [Michael Jahn](https://github.com/micjahn).

ZXing.Net.Mobile uses ZXing.Net to detect and generate barcodes by providing UI related API's to load camara previews in realtime to detect barcodes from the camera preview feed.

## Why binaries?

While ZXing.Net does ship as a nuget package, it unfortunately has an [issue with bait and switch](https://github.com/micjahn/ZXing.Net/issues/249).  Until this is resolved it is easiest to just consume the netstandard2.0 implementation binary directly in this repo.
