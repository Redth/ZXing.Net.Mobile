using System;
using Android.Content;
using ZXing.Mobile.Detectors;
using Android.Gms.Vision;
using Android.Gms.Vision.Barcodes;
using Android.Graphics;
using Java.Nio;
using ZXing.Mobile;

namespace ZXing.Net.Mobile.Android.Vision
{
    public class GoogleVisionDetector : IDetector
    {
        private readonly Context _context;
        private BarcodeDetector _detector;

        public GoogleVisionDetector(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Use this method to ensure that lib is included by linker (usually in LinkerPleaseInclude.cs)
        /// </summary>
        public static void Init()
        {
        }

        public bool Init(MobileBarcodeScanningOptions scanningOptions)
        {
            // todo: associate ZXing formats with Vision to use .SetBarcodeFormats(..)
            _detector = new BarcodeDetector.Builder(_context).Build();

            if (!_detector.IsOperational)
            {
                // Note: The first time that an app using the barcode or face API is installed on a
                // device, GMS will download a native libraries to the device in order to do detection.
                // Usually this completes before the app is run for the first time.  But if that
                // download has not yet completed, then the above call will not detect any barcodes
                // and/or faces.
                //
                // isOperational() can be used to check if the required native libraries are currently
                // available.  The detectors will automatically become operational once the library
                // downloads complete on device.
                global::Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Detector dependencies are not yet available.");
                return false;
            }

            return true;
        }

        public Result Decode(byte[] bytes, int width, int height)
        {
            var frame =
                new Frame.Builder().SetImageData(ByteBuffer.Wrap(bytes), width, height, (int) ImageFormatType.Nv21)
                    .Build();
            var detectedItems = _detector.Detect(frame);
            if (detectedItems.Size() > 0)
            {
                var result = (Barcode) detectedItems.ValueAt(0);
                if (result != null)
                {
                    return new Result(result.RawValue, bytes, new[]
                    {
                        new ResultPoint(result.BoundingBox.Left, result.BoundingBox.Top),
                        new ResultPoint(result.BoundingBox.Right, result.BoundingBox.Bottom)
                    }, VisionToZxingFormat(result.Format));
                }
            }
            return null;
        }

        private BarcodeFormat VisionToZxingFormat(global::Android.Gms.Vision.Barcodes.BarcodeFormat format)
        {
            switch (format)
            {
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.Code128:
                    return BarcodeFormat.CODE_128;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.Code39:
                    return BarcodeFormat.CODE_39;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.Code93:
                    return BarcodeFormat.CODE_93;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.Codabar:
                    return BarcodeFormat.CODABAR;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.DataMatrix:
                    return BarcodeFormat.DATA_MATRIX;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.Ean13:
                    return BarcodeFormat.EAN_13;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.Ean8:
                    return BarcodeFormat.EAN_8;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.Itf:
                    return BarcodeFormat.ITF;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.QrCode:
                    return BarcodeFormat.QR_CODE;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.UpcA:
                    return BarcodeFormat.UPC_A;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.UpcE:
                    return BarcodeFormat.UPC_E;
                case global::Android.Gms.Vision.Barcodes.BarcodeFormat.Pdf417:
                    return BarcodeFormat.PDF_417;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}