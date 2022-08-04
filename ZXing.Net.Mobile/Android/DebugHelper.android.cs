using System.IO;
using System.Net.Http;
using Android.Graphics;
using ZXing.Mobile.CameraAccess;

namespace ZXing.Net.Mobile.Android
{
#if DEBUG
    // Do not use this in production.
    // This is a simple helping class to use with the ImageReceiver Tool
    public static class DebugHelper
    {
        static int sendThreshold = 18;
        static int sendCount;

        // Used to check NV21 Output
        static byte[] NV21toJPEG(byte[] nv21, int width, int height)
        {
            using (var mems = new MemoryStream())
            {
                var yuv = new YuvImage(nv21, ImageFormatType.Nv21, width, height, null);
                yuv.CompressToJpeg(new Rect(0, 0, width, height), 100, mems);
                return mems.ToArray();
            }
        }

        public static void SendBytesToEndpoint(byte[] data, string endpoint, bool ignoreThreshold = false, params (string Key, string Value)[] headers)
        {
            if (!ignoreThreshold)
            {
                if (sendCount < sendThreshold)
                {
                    sendCount++;
                    return;
                }

                sendCount = 0;
            }

            var httpClient = GetHttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new ByteArrayContent(data);

            foreach (var header in headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            httpClient.SendAsync(request);
        }

        public static void SendNV21toJPEGToEndpoint(byte[] data, int width, int height, string endpoint, bool ignoreThreshold = false, params (string Key, string Value)[] headers)
        {
            var jpeg = NV21toJPEG(data, width, height);
            SendBytesToEndpoint(jpeg, endpoint, ignoreThreshold, headers);
        }

        public static void SendNV21toJPEGToEndpoint(CapturedImageData data, string endpoint, bool ignoreThreshold = false, params (string Key, string Value)[] headers)
            => SendNV21toJPEGToEndpoint(data.Matrix, data.Width, data.Height, endpoint, ignoreThreshold, headers);

        static HttpClient GetHttpClient()
        {
            var handler = new HttpClientHandler();
            // we dont need to validate certificates
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;

            return new HttpClient(handler);
        }
    }
#endif
}
