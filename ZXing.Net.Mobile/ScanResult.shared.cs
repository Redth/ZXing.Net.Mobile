using System.Collections.Generic;
using ZXing;

namespace ZXing.Mobile
{
    public class ScanResult : IScanResult
    {
        public byte[,] ImageBytes { get; }

        public string Text { get; }
        public byte[] RawBytes { get; }
        public ResultPoint[] ResultPoints { get; }
        public BarcodeFormat BarcodeFormat { get; }
        public IDictionary<ResultMetadataType, object> ResultMetadata { get; }
        public long Timestamp { get; }
        public int NumBits { get; }

        public ScanResult(ZXing.Result result, byte[,] imageBytes)
        {
            ImageBytes = imageBytes;
            BarcodeFormat = result.BarcodeFormat;
            Text = result.Text;
            ResultPoints = result.ResultPoints;
            RawBytes = result.RawBytes;
            ResultMetadata = result.ResultMetadata;
            Timestamp = result.Timestamp;
            NumBits = result.NumBits;
        }
    }
}