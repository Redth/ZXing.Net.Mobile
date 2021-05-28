using System.Collections.Generic;
using ZXing;

namespace ZXing.Mobile
{
    public interface IScanResult
    {
        BarcodeFormat BarcodeFormat { get; }
        byte[,] ImageBytes { get; }
        int NumBits { get; }
        byte[] RawBytes { get; }
        IDictionary<ResultMetadataType, object> ResultMetadata { get; }
        ResultPoint[] ResultPoints { get; }
        string Text { get; }
        long Timestamp { get; }
    }
}