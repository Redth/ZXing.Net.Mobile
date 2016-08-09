using System;
using System.Collections.Generic;
using NUnit.Framework;
using UITests.Shared;
using ZXing;

namespace UITests
{
    public partial class ScanningTests
    {
        [Test]
        public void Scan_QRCode_Succeeds ()
        {
            const BarcodeFormat FORMAT = BarcodeFormat.QR_CODE;
            const string VALUE = "Xamarin";

            app.DisplayBarcode (FORMAT, VALUE);
            app.InvokeScanner (FORMAT, platform);

            app.AssertUITestBackdoorResult (FORMAT, VALUE);
        }

        [Test]
        public void Scan_PDF417_Succeeds ()
        {
            const BarcodeFormat FORMAT = BarcodeFormat.PDF_417;
            const string VALUE = "Xamarin";

            app.DisplayBarcode (FORMAT, VALUE);
            app.InvokeScanner (FORMAT, platform);

            app.AssertUITestBackdoorResult (FORMAT, VALUE);
        }
    }
}

