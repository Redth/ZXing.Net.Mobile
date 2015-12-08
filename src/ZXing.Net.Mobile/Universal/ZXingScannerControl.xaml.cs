using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ZXing.Mobile;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ZXing.Mobile
{
    public sealed partial class ZXingScannerControl : UserControl, IDisposable
    {
        public ZXingScannerControl()
        {
            this.InitializeComponent();
        }

        public async Task StartScanning(Action<ZXing.Result> scanCallback, MobileBarcodeScanningOptions options = null)
        {
            ScanCallback = scanCallback;
            ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

            topText.Text = TopText ?? string.Empty;
            bottomText.Text = BottomText ?? string.Empty;

            if (UseCustomOverlay && CustomOverlay != null)
            {
                gridCustomOverlay.Children.Clear();
                gridCustomOverlay.Children.Add(CustomOverlay);

                gridCustomOverlay.Visibility = Visibility.Visible;
                gridDefaultOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                gridCustomOverlay.Visibility = Visibility.Collapsed;
                gridDefaultOverlay.Visibility = Visibility.Visible;
            }

            // Find which device to use
            var preferredCamera = await this.GetFilteredCameraOrDefaultAsync(ScanningOptions);            
            mediaCapture = new MediaCapture();
            
            // Initialize the capture with the settings above
            await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                VideoDeviceId = preferredCamera.Id
            });

            // Set the capture element's source to show it in the UI
            captureElement.Source = mediaCapture;

            // Start the preview
            await mediaCapture.StartPreviewAsync();
            
            // Get all the available resolutions for preview
            var availableProperties = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
            var availableResolutions = new List<CameraResolution>();
            foreach (var ap in availableProperties)
            {
                var vp = (VideoEncodingProperties)ap;
                availableResolutions.Add(new CameraResolution { Width = (int)vp.Width, Height = (int)vp.Height });                
            }
            var previewResolution = availableResolutions.FirstOrDefault();
            if (ScanningOptions.CameraResolutionSelector != null)
                previewResolution = ScanningOptions.CameraResolutionSelector(availableResolutions);

            // Find the matching property based on the selection, again
            var chosenProp = availableProperties.FirstOrDefault(ap => ((VideoEncodingProperties)ap).Width == previewResolution.Width && ((VideoEncodingProperties)ap).Height == previewResolution.Height);

            // Set the selected resolution
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, chosenProp);

            // Get our preview properties
            var previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            
            // Setup a frame to use as the input settings
            var destFrame = new VideoFrame(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

            var zxing = ScanningOptions.BuildBarcodeReader();

            timerPreview = new Timer(async (state) => {
                if (stopping)
                    return;
                if (mediaCapture == null || mediaCapture.CameraStreamState != Windows.Media.Devices.CameraStreamState.Streaming)
                    return;

                // Get preview 
                var frame = await mediaCapture.GetPreviewFrameAsync(destFrame);

                // Create our luminance source
                var luminanceSource = new SoftwareBitmapLuminanceSource(frame.SoftwareBitmap);

                // Try decoding the image
                var result = zxing.Decode(luminanceSource);

                // Check if a result was found
                if (result != null && !string.IsNullOrEmpty (result.Text))
                {
                    if (!ContinuousScanning)
                        await StopScanning();
                    LastScanResult = result;
                    ScanCallback(result);                    
                }               
            }, null, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200));           
        }

        async Task<DeviceInformation> GetFilteredCameraOrDefaultAsync(MobileBarcodeScanningOptions options)
        {
            var videoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            var useFront = options.UseFrontCameraIfAvailable.HasValue && options.UseFrontCameraIfAvailable.Value;

            var selectedCamera = videoCaptureDevices.FirstOrDefault(vcd => vcd.EnclosureLocation != null
                && ((!useFront && vcd.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back)
                    || (useFront && vcd.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front)));


            // we fall back to the first camera that we can find.  
            if (selectedCamera == null)
                selectedCamera = videoCaptureDevices.FirstOrDefault();

            return (selectedCamera);
        }

        Timer timerPreview;
        MediaCapture mediaCapture;

        bool stopping = false;

        public Action<Result> ScanCallback { get; set; }
        public MobileBarcodeScanningOptions ScanningOptions { get; set; }
        public MobileBarcodeScannerBase Scanner { get; set; }
        public UIElement CustomOverlay { get; set; }
        public string TopText { get; set; }
        public string BottomText { get; set; }
        public bool UseCustomOverlay { get; set; }
        public bool ContinuousScanning { get; set; }

        public Result LastScanResult { get; set; }
        

        public bool IsTorchOn
        {
            get { return false; }
        }

        public void Torch(bool on)
        {
            
        }

        public void ToggleTorch()
        {
            
        }

        public void AutoFocus()
        {
            
        }

        public async Task StopScanning()
        {
            stopping = true;
            await mediaCapture.StopPreviewAsync();
            if (UseCustomOverlay && CustomOverlay != null)
                gridCustomOverlay.Children.Remove(CustomOverlay);

            timerPreview.Change(Timeout.Infinite, Timeout.Infinite);
            stopping = false;
            //TODO: Stop
        }

        public async Task Cancel()
        {
            LastScanResult = null;

           await StopScanning();

            if (ScanCallback != null)
                ScanCallback(null);
        }        

        public async void Dispose()
        {
            await StopScanning();
            this.gridCustomOverlay.Children.Clear();            
        }

        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            base.OnTapped(e);

            //TODO: Focus
        }
        //protected override void OnTap(System.Windows.Input.GestureEventArgs e)
        //{
        //    base.OnTap(e);

        //    if (_reader != null)
        //    {
        //        //var pos = e.GetPosition(this);
        //        _reader.Focus();
        //    }
        //}

        private void buttonToggleFlash_Click(object sender, RoutedEventArgs e)
        {
            ToggleTorch();
        }
    }
}
