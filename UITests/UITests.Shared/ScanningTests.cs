using System;
using NUnit.Framework;
using UITests.Shared;

namespace UITests
{
    public partial class ScanningTests
    {
        [Test]
        public void Scan_QRCode_Succeeds ()
        {
            const string FORMAT = "QR_CODE";
            const string VALUE = "Xamarin";

            app.DisplayBarcode (FORMAT, VALUE);
            app.InvokeScanner ("QR_CODE", platform);

            app.AssertUITestBackdoorResult (FORMAT, VALUE);
        }
    }
}

