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
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.System.Display;
using Windows.UI.Core;
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
    public sealed partial class ZXingScannerControl : UserControl, IScannerView, IDisposable
    {
        public ZXingScannerControl()
        {
            this.InitializeComponent();

            displayOrientation = displayInformation.CurrentOrientation;
            displayInformation.OrientationChanged += displayInformation_OrientationChanged; 
        }

        async void displayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            displayOrientation = sender.CurrentOrientation;
            await SetPreviewRotationAsync();
        }

        // Receive notifications about rotation of the UI and apply any necessary rotation to the preview stream
        readonly DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
        DisplayOrientations displayOrientation = DisplayOrientations.Portrait;

        // Rotation metadata to apply to the preview stream (MF_MT_VIDEO_ROTATION)
        // Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
        static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        // Prevent the screen from sleeping while the camera is running
        readonly DisplayRequest _displayRequest = new DisplayRequest();

        // For listening to media property changes
        readonly SystemMediaTransportControls _systemMediaControls = SystemMediaTransportControls.GetForCurrentView();


        public async void StartScanning (Action<ZXing.Result> scanCallback, MobileBarcodeScanningOptions options = null)
        {
            await StartScanningAsync(scanCallback, options);
        }

        public async void StopScanning ()
        {
            await StopScanningAsync();
        }

        public void PauseAnalysis ()
        {
            isAnalyzing = false;
        }

        public void ResumeAnalysis ()
        {
            
            isAnalyzing = true;
        }

        public bool IsAnalyzing
        {
            get { return isAnalyzing; }
        }

        public async Task StartScanningAsync(Action<ZXing.Result> scanCallback, MobileBarcodeScanningOptions options = null)
        {
            if (stopping)
                return;

            isAnalyzing = true;
            ScanCallback = scanCallback;
            ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

            topText.Text = TopText ?? string.Empty;
            bottomText.Text = BottomText ?? string.Empty;

            if (UseCustomOverlay)
            {
                gridCustomOverlay.Children.Clear();
                if (CustomOverlay != null)
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
            var preferredCamera = await GetFilteredCameraOrDefaultAsync(ScanningOptions);
            if (preferredCamera == null)
            {
                System.Diagnostics.Debug.WriteLine("No camera available");
                isMediaCaptureInitialized = false;
                return;
            }

            mediaCapture = new MediaCapture();

            // Initialize the capture with the settings above
            try
            {
                await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    VideoDeviceId = preferredCamera.Id
                });
                isMediaCaptureInitialized = true;
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("Denied access to the camera");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception when init MediaCapture: {0}", ex);
            }

            if (!isMediaCaptureInitialized)
                return;

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
                System.Diagnostics.Debug.WriteLine("Camera Preview Resolution: {0}x{1}", vp.Width, vp.Height);
                availableResolutions.Add(new CameraResolution { Width = (int)vp.Width, Height = (int)vp.Height });                
            }
            CameraResolution previewResolution = null;
            if (ScanningOptions.CameraResolutionSelector != null)
                previewResolution = ScanningOptions.CameraResolutionSelector(availableResolutions);

            // If the user did not specify a resolution, let's try and find a suitable one
            if (previewResolution == null)
            {
                // Loop through all supported sizes
                foreach (var sps in availableResolutions)
                {
                    // Find one that's >= 640x360 but <= 1000x1000
                    // This will likely pick the *smallest* size in that range, which should be fine
                    if (sps.Width >= 640 && sps.Width <= 1000 && sps.Height >= 360 && sps.Height <= 1000)
                    {
                        previewResolution = new CameraResolution
                        {
                            Width = sps.Width,
                            Height = sps.Height
                        };
                        break;
                    }
                }
            }

            if (previewResolution == null)
                previewResolution = availableResolutions.LastOrDefault();

            System.Diagnostics.Debug.WriteLine("Using Preview Resolution: {0}x{1}", previewResolution.Width, previewResolution.Height);

            // Find the matching property based on the selection, again
            var chosenProp = availableProperties.FirstOrDefault(ap => ((VideoEncodingProperties)ap).Width == previewResolution.Width && ((VideoEncodingProperties)ap).Height == previewResolution.Height);
            
            // Set the selected resolution
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, chosenProp);

            await SetPreviewRotationAsync();

            await SetupAutoFocus();

            captureElement.Stretch = Stretch.UniformToFill;

            // Get our preview properties
            var previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            
            // Setup a frame to use as the input settings
            var destFrame = new VideoFrame(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

            var zxing = ScanningOptions.BuildBarcodeReader();

            timerPreview = new Timer(async (state) => {

                var delay = ScanningOptions.DelayBetweenAnalyzingFrames;

                if (stopping || processing || !isAnalyzing
                || (mediaCapture == null || mediaCapture.CameraStreamState != Windows.Media.Devices.CameraStreamState.Streaming))
                {
                    timerPreview.Change(delay, Timeout.Infinite);
                    return;
                }

                processing = true;

                SoftwareBitmapLuminanceSource luminanceSource = null;

                try
                {

                    // Get preview 
                    var frame = await mediaCapture.GetPreviewFrameAsync(destFrame);

                    // Create our luminance source
                    luminanceSource = new SoftwareBitmapLuminanceSource(frame.SoftwareBitmap);

                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("GetPreviewFrame Failed: {0}", ex);
                }

                ZXing.Result result = null;

                try
                {
                    // Try decoding the image
                    if (luminanceSource != null)
                        result = zxing.Decode(luminanceSource);
                }
                catch (Exception ex)
                {
                    
                }

                // Check if a result was found
                if (result != null && !string.IsNullOrEmpty (result.Text))
                {
                    if (!ContinuousScanning)
                    {
                        delay = Timeout.Infinite;
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { await StopScanningAsync(); });
                    }
                    else
                    {
                        delay = ScanningOptions.DelayBetweenContinuousScans;
                    }

                    LastScanResult = result;
                    ScanCallback(result);
                }

                processing = false;

                timerPreview.Change(delay, Timeout.Infinite);
                         
            }, null, ScanningOptions.InitialDelayBeforeAnalyzingFrames, Timeout.Infinite);
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
            {
                var whichCamera = useFront ? "front" : "back";
                System.Diagnostics.Debug.WriteLine("Finding " + whichCamera + " camera failed, opening first available camera");
                selectedCamera = videoCaptureDevices.FirstOrDefault();
            }

            return selectedCamera;
        }

        protected override async void OnPointerPressed(PointerRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AutoFocus requested");
            base.OnPointerPressed(e);
            var pt = e.GetCurrentPoint(captureElement);
            await AutoFocusAsync((int)pt.Position.X, (int)pt.Position.Y, true);
        }

        Timer timerPreview;
        MediaCapture mediaCapture;

        bool stopping = false;
        bool isMediaCaptureInitialized = false;

        volatile bool processing = false;
        volatile bool isAnalyzing = false;

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
            get
            {
                return HasTorch && mediaCapture.VideoDeviceController.TorchControl.Enabled;
            }
        }

        public bool IsFocusSupported
        {
            get
            {
                return mediaCapture != null
                    && mediaCapture.VideoDeviceController != null
                    && mediaCapture.VideoDeviceController.FocusControl != null
                    && mediaCapture.VideoDeviceController.FocusControl.Supported;
            }
        }

        private async Task SetupAutoFocus()
        {
            if (IsFocusSupported)
            {
                var focusControl = mediaCapture.VideoDeviceController.FocusControl;

                var focusSettings = new FocusSettings();
                focusSettings.AutoFocusRange = focusControl.SupportedFocusRanges.Contains(AutoFocusRange.FullRange)
                    ? AutoFocusRange.FullRange
                    : focusControl.SupportedFocusRanges.FirstOrDefault();

                var supportedFocusModes = focusControl.SupportedFocusModes;
                if (supportedFocusModes.Contains(FocusMode.Continuous))
                {
                    focusSettings.Mode = FocusMode.Continuous;
                }
                else if (supportedFocusModes.Contains(FocusMode.Auto))
                {
                    focusSettings.Mode = FocusMode.Auto;
                }

                if (focusSettings.Mode == FocusMode.Continuous || focusSettings.Mode == FocusMode.Auto)
                {
                    //await focusControl.UnlockAsync();
                    focusSettings.WaitForFocus = false;
                    focusControl.Configure(focusSettings);
                    await focusControl.FocusAsync();
                }
            }
        }

        public void Torch(bool on)
        {
            if (HasTorch)
                mediaCapture.VideoDeviceController.TorchControl.Enabled = on;
        }

        public void ToggleTorch()
        {
            if (HasTorch)
                Torch(!IsTorchOn);
        }

        public bool HasTorch
        {
            get
            {
                return mediaCapture != null
                    && mediaCapture.VideoDeviceController != null
                    && mediaCapture.VideoDeviceController.TorchControl != null
                    && mediaCapture.VideoDeviceController.TorchControl.Supported;
            }
        }

        public async void AutoFocus ()
        {
            await AutoFocusAsync(0, 0, false);
        }

        public async void AutoFocus (int x, int y)
        {
            await AutoFocusAsync(x, y, true);
        }

        public async Task AutoFocusAsync(int x, int y, bool useCoordinates)
        {
            if (IsFocusSupported)
            {
                var focusControl = mediaCapture.VideoDeviceController.FocusControl;

                try
                {
                    // todo
                    //await focusControl.LockAsync();
                    //var roiControl = mediaCapture.VideoDeviceController.RegionsOfInterestControl;
                    //if (useCoordinates)
                    //{
                    //    await roiControl.SetRegionsAsync(new[] { region }, true);

                    //    var focusRange = focusControl.SupportedFocusRanges.Contains(AutoFocusRange.FullRange)
                    //        ? AutoFocusRange.FullRange
                    //        : focusControl.SupportedFocusRanges.FirstOrDefault();

                    //    var focusMode = focusControl.SupportedFocusModes.Contains(FocusMode.Single)
                    //        ? FocusMode.Single
                    //        : focusControl.SupportedFocusModes.FirstOrDefault();
                    //    var settings = new FocusSettings
                    //    {
                    //        Mode = focusMode,
                    //        AutoFocusRange = focusRange
                    //    };

                    //    focusControl.Configure(settings);
                    //}
                    //else
                    //{
                    //    // If no region provided, clear any regions and reset focus
                    //    await roiControl.ClearRegionsAsync();
                    //}

                    await focusControl.FocusAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AutoFocusAsync Error: {0}", ex);
                }
            }
        }

        public async Task StopScanningAsync()
        {
            stopping = true;
            isAnalyzing = false;

            try
            {
                if (isMediaCaptureInitialized)
                    await mediaCapture.StopPreviewAsync();
                if (UseCustomOverlay && CustomOverlay != null)
                    gridCustomOverlay.Children.Remove(CustomOverlay);
            }
            catch { }
            finally {
                //second execution from sample will crash if the object is not properly disposed (always on mobile, sometimes on desktop)
                 if (mediaCapture != null)
                    mediaCapture.Dispose();
            }

            //this solves a crash occuring when the user rotates the screen after the QR scanning is closed
            displayInformation.OrientationChanged -= displayInformation_OrientationChanged;

            if (timerPreview != null)
                timerPreview.Change(Timeout.Infinite, Timeout.Infinite);
            stopping = false;            
        }

        public async Task Cancel()
        {
            LastScanResult = null;

           await StopScanningAsync();

            if (ScanCallback != null)
                ScanCallback(null);
        }        

        public async void Dispose()
        {
            await StopScanningAsync();
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


        /// <summary>
        /// Gets the current orientation of the UI in relation to the device and applies a corrective rotation to the preview
        /// </summary>
        private async Task SetPreviewRotationAsync()
        {
            // Only need to update the orientation if the camera is mounted on the device
            //if (_externalCamera) return;

            // Calculate which way and how far to rotate the preview
            int rotationDegrees = ConvertDisplayOrientationToDegrees(displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored
            //if (_mirroringPreview)
            //{
            //    rotationDegrees = (360 - rotationDegrees) % 360;
            //}

            // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
            var props = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        /// <summary>
        /// Converts the given orientation of the app on the screen to the corresponding rotation in degrees
        /// </summary>
        /// <param name="orientation">The orientation of the app on the screen</param>
        /// <returns>An orientation in degrees</returns>
        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }
    }
}
