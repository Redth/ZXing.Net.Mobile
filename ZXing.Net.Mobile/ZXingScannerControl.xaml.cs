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
	public sealed partial class ZXingScannerControl : UserControl, IScannerView, IScannerSessionHost, IDisposable
	{
		public ZXingScannerControl()
		{
			InitializeComponent();

			displayOrientation = displayInformation.CurrentOrientation;
			displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
		}

		public event ScannerOpened OnCameraInitialized;
		public delegate void ScannerOpened();

		public event ScannerError OnScannerError;
		public delegate void ScannerError(IEnumerable<string> errors);

		async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
		{
			//This safeguards against a null reference if the device is rotated *before* the first call to StartScanning
			if (mediaCapture == null) return;

			displayOrientation = sender.CurrentOrientation;
			var props = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
			await SetPreviewRotationAsync(props);
		}

		// Receive notifications about rotation of the UI and apply any necessary rotation to the preview stream
		readonly DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
		DisplayOrientations displayOrientation = DisplayOrientations.Portrait;
		VideoFrame videoFrame;

		// Information about the camera device.
		bool mirroringPreview = false;
		bool externalCamera = false;

		// Rotation metadata to apply to the preview stream (MF_MT_VIDEO_ROTATION)
		// Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
		static readonly Guid rotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

		// Prevent the screen from sleeping while the camera is running
		readonly DisplayRequest displayRequest = new DisplayRequest();

		// For listening to media property changes
		readonly SystemMediaTransportControls systemMediaControls = SystemMediaTransportControls.GetForCurrentView();


		public async void StartScanning(Action<ZXing.Result> scanCallback, MobileBarcodeScanningOptions options = null)
			=> await StartScanningAsync(scanCallback, options);

		public async void StopScanning()
			=> await StopScanningAsync();

		public void PauseAnalysis()
			=> isAnalyzing = false;

		public void ResumeAnalysis()
			=> isAnalyzing = true;

		public bool IsAnalyzing
			=> isAnalyzing;

		public async Task StartScanningAsync(Action<ZXing.Result> scanCallback, MobileBarcodeScanningOptions options = null)
		{
			if (stopping)
			{
				var error = "Camera is closing";
				OnScannerError?.Invoke(new[] { error });
				return;
			}


			displayRequest.RequestActive();

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
				var error = "No camera available";
				System.Diagnostics.Debug.WriteLine(error);
				isMediaCaptureInitialized = false;
				OnScannerError?.Invoke(new[] { error });
				return;
			}

			if (preferredCamera.EnclosureLocation == null || preferredCamera.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
			{
				// No information on the location of the camera, assume it's an external camera, not integrated on the device.
				externalCamera = true;
			}
			else
			{
				// Camera is fixed on the device.
				externalCamera = false;

				// Only mirror the preview if the camera is on the front panel.
				mirroringPreview = preferredCamera.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front;
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
			{
				var error = "Unexpected error on Camera initialisation";
				OnScannerError?.Invoke(new[] { error });
				return;
			}


			// Set the capture element's source to show it in the UI
			captureElement.Source = mediaCapture;
			captureElement.FlowDirection = mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

			try
			{
				// Start the preview
				await mediaCapture.StartPreviewAsync();
			}
			catch (Exception ex)
			{
				var error = "Unexpected error on Camera initialisation";
				OnScannerError?.Invoke(new[] { error });
				return;
			}

			if (mediaCapture.CameraStreamState == CameraStreamState.Streaming)
			{
				OnCameraInitialized?.Invoke();
			}

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

			if (availableResolutions == null || availableResolutions.Count < 1)
			{
				var error = "Camera is busy. Try to close all applications that use camera.";
				OnScannerError?.Invoke(new[] { error });
				return;
			}

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

			if (previewResolution == null)
			{
				System.Diagnostics.Debug.WriteLine("No preview resolution available. Camera may be in use by another application.");
				return;
			}

			MobileBarcodeScanner.Log("Using Preview Resolution: {0}x{1}", previewResolution.Width, previewResolution.Height);

			// Find the matching property based on the selection, again
			var chosenProp = availableProperties.FirstOrDefault(ap => ((VideoEncodingProperties)ap).Width == previewResolution.Width && ((VideoEncodingProperties)ap).Height == previewResolution.Height);

			// Pass in the requested preview size properties
			// so we can set them at the same time as the preview rotation
			// to save an additional set property call
			await SetPreviewRotationAsync(chosenProp);

			// *after* the preview is setup, set this so that the UI layout happens
			// otherwise the preview gets stuck in a funny place on screen
			captureElement.Stretch = Stretch.UniformToFill;

			await SetupAutoFocus();

			var zxing = ScanningOptions.BuildBarcodeReader();

			timerPreview = new Timer(async (state) =>
			{

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
					var frame = await mediaCapture.GetPreviewFrameAsync(videoFrame);

					// Create our luminance source
					luminanceSource = new SoftwareBitmapLuminanceSource(frame.SoftwareBitmap);

				}
				catch (Exception ex)
				{
					MobileBarcodeScanner.Log("GetPreviewFrame Failed: {0}", ex);
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
					MobileBarcodeScanner.Log("Warning: zxing.Decode Failed: {0}", ex);
				}

				// Check if a result was found
				if (result != null && !string.IsNullOrEmpty(result.Text))
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

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("AutoFocus requested");
			base.OnPointerPressed(e);
			var pt = e.GetCurrentPoint(captureElement);
			new Task(() => { AutoFocusAsync((int)pt.Position.X, (int)pt.Position.Y, true); });
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
			=> HasTorch && mediaCapture.VideoDeviceController.TorchControl.Enabled;

		public bool IsFocusSupported
			=> mediaCapture != null
					&& isMediaCaptureInitialized
					&& mediaCapture.VideoDeviceController != null
					&& mediaCapture.VideoDeviceController.FocusControl != null
					&& mediaCapture.VideoDeviceController.FocusControl.Supported;

		async Task SetupAutoFocus()
		{
			if (IsFocusSupported)
			{
				var focusControl = mediaCapture.VideoDeviceController.FocusControl;

				var focusSettings = new FocusSettings();

				if (ScanningOptions.DisableAutofocus)
				{
					focusSettings.Mode = FocusMode.Manual;
					focusSettings.Distance = ManualFocusDistance.Nearest;
					focusControl.Configure(focusSettings);
					return;
				}

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
			=> mediaCapture != null
					&& mediaCapture.VideoDeviceController != null
					&& mediaCapture.VideoDeviceController.TorchControl != null
					&& mediaCapture.VideoDeviceController.TorchControl.Supported;

		public async void AutoFocus()
			=> await AutoFocusAsync(0, 0, false);

		public async void AutoFocus(int x, int y)
			=> await AutoFocusAsync(x, y, true);

		public async Task AutoFocusAsync(int x, int y, bool useCoordinates)
		{
			if (ScanningOptions.DisableAutofocus)
				return;

			if (IsFocusSupported && mediaCapture?.CameraStreamState == CameraStreamState.Streaming)
			{
				var focusControl = mediaCapture.VideoDeviceController.FocusControl;
				var roiControl = mediaCapture.VideoDeviceController.RegionsOfInterestControl;
				try
				{
					if (roiControl.AutoFocusSupported && roiControl.MaxRegions > 0)
					{
						if (useCoordinates)
						{
							var props = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);

							var previewEncodingProperties = GetPreviewResolution(props);
							var previewRect = GetPreviewStreamRectInControl(previewEncodingProperties, captureElement);
							var focusPreview = ConvertUiTapToPreviewRect(new Point(x, y), new Size(20, 20), previewRect);
							var regionOfInterest = new RegionOfInterest
							{
								AutoFocusEnabled = true,
								BoundsNormalized = true,
								Bounds = focusPreview,
								Type = RegionOfInterestType.Unknown,
								Weight = 100
							};
							await roiControl.SetRegionsAsync(new[] { regionOfInterest }, true);

							var focusRange = focusControl.SupportedFocusRanges.Contains(AutoFocusRange.FullRange)
								? AutoFocusRange.FullRange
								: focusControl.SupportedFocusRanges.FirstOrDefault();

							var focusMode = focusControl.SupportedFocusModes.Contains(FocusMode.Single)
								? FocusMode.Single
								: focusControl.SupportedFocusModes.FirstOrDefault();

							var settings = new FocusSettings
							{
								Mode = focusMode,
								AutoFocusRange = focusRange,
							};

							focusControl.Configure(settings);
						}
						else
						{
							// If no region provided, clear any regions and reset focus
							await roiControl.ClearRegionsAsync();
						}
					}

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
			if (stopping)
				return;

			stopping = true;
			isAnalyzing = false;

			try
			{
				displayRequest?.RequestRelease();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Release Request Failed: {ex}");
			}

			try
			{
				if (IsTorchOn)
					Torch(false);
				if (isMediaCaptureInitialized)
					await mediaCapture.StopPreviewAsync();
				if (UseCustomOverlay && CustomOverlay != null)
					gridCustomOverlay.Children.Remove(CustomOverlay);
			}
			catch { }
			finally
			{
				//second execution from sample will crash if the object is not properly disposed (always on mobile, sometimes on desktop)
				if (mediaCapture != null)
					mediaCapture.Dispose();
			}

			//this solves a crash occuring when the user rotates the screen after the QR scanning is closed
			displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;

			if (timerPreview != null)
				timerPreview.Change(Timeout.Infinite, Timeout.Infinite);
			stopping = false;
		}

		public async Task Cancel()
		{
			LastScanResult = null;

			await StopScanningAsync();

			ScanCallback?.Invoke(null);
		}

		public async void Dispose()
		{
			await StopScanningAsync();
			gridCustomOverlay?.Children?.Clear();
		}

		protected override void OnTapped(TappedRoutedEventArgs e)
			=> base.OnTapped(e);

		void ButtonToggleFlash_Click(object sender, RoutedEventArgs e)
			=> ToggleTorch();

		/// <summary>
		/// Gets the current orientation of the UI in relation to the device and applies a corrective rotation to the preview
		/// </summary>
		async Task SetPreviewRotationAsync(IMediaEncodingProperties props)
		{
			// Only need to update the orientation if the camera is mounted on the device.
			if (mediaCapture == null)
				return;

			// Calculate which way and how far to rotate the preview.
			CalculatePreviewRotation(out var sourceRotation, out var rotationDegrees);

			// Set preview rotation in the preview source.
			mediaCapture.SetPreviewRotation(sourceRotation);

			// Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
			//var props = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
			props.Properties.Add(rotationKey, rotationDegrees);
			await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, props);

			var currentPreviewResolution = GetPreviewResolution(props);
			// Setup a frame to use as the input settings
			videoFrame = new VideoFrame(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, (int)currentPreviewResolution.Width, (int)currentPreviewResolution.Height);
		}

		Size GetPreviewResolution(IMediaEncodingProperties props)
		{
			// Get our preview properties
			if (props is VideoEncodingProperties previewProperties)
			{
				var streamWidth = previewProperties.Width;
				var streamHeight = previewProperties.Height;

				// For portrait orientations, the width and height need to be swapped
				if (displayOrientation == DisplayOrientations.Portrait || displayOrientation == DisplayOrientations.PortraitFlipped)
				{
					streamWidth = previewProperties.Height;
					streamHeight = previewProperties.Width;
				}

				return new Size(streamWidth, streamHeight);
			}

			return default;
		}

		/// <summary>
		/// Reads the current orientation of the app and calculates the VideoRotation necessary to ensure the preview is rendered in the correct orientation.
		/// </summary>
		/// <param name="sourceRotation">The rotation value to use in MediaCapture.SetPreviewRotation.</param>
		/// <param name="rotationDegrees">The accompanying rotation metadata with which to tag the preview stream.</param>
		void CalculatePreviewRotation(out VideoRotation sourceRotation, out int rotationDegrees)
		{
			// Note that in some cases, the rotation direction needs to be inverted if the preview is being mirrored.
			switch (displayInformation.CurrentOrientation)
			{
				case DisplayOrientations.Portrait:
					if (mirroringPreview)
					{
						rotationDegrees = 270;
						sourceRotation = VideoRotation.Clockwise270Degrees;
					}
					else
					{
						rotationDegrees = 90;
						sourceRotation = VideoRotation.Clockwise90Degrees;
					}
					break;

				case DisplayOrientations.LandscapeFlipped:
					// No need to invert this rotation, as rotating 180 degrees is the same either way.
					rotationDegrees = 180;
					sourceRotation = VideoRotation.Clockwise180Degrees;
					break;

				case DisplayOrientations.PortraitFlipped:
					if (mirroringPreview)
					{
						rotationDegrees = 90;
						sourceRotation = VideoRotation.Clockwise90Degrees;
					}
					else
					{
						rotationDegrees = 270;
						sourceRotation = VideoRotation.Clockwise270Degrees;
					}
					break;

				case DisplayOrientations.Landscape:
				default:
					rotationDegrees = 0;
					sourceRotation = VideoRotation.None;
					break;
			}
		}

		/// <summary>
		/// Applies the necessary rotation to a tap on a CaptureElement (with Stretch mode set to Uniform) to account for device orientation
		/// </summary>
		/// <param name="tap">The location, in UI coordinates, of the user tap</param>
		/// <param name="size">The size, in UI coordinates, of the desired focus rectangle</param>
		/// <param name="previewRect">The area within the CaptureElement that is actively showing the preview, and is not part of the letterboxed area</param>
		/// <returns>A Rect that can be passed to the MediaCapture Focus and RegionsOfInterest APIs, with normalized bounds in the orientation of the native stream</returns>
		Rect ConvertUiTapToPreviewRect(Point tap, Size size, Rect previewRect)
		{
			// Adjust for the resulting focus rectangle to be centered around the position
			double left = tap.X - size.Width / 2, top = tap.Y - size.Height / 2;

			// Get the information about the active preview area within the CaptureElement (in case it's letterboxed)
			double previewWidth = previewRect.Width, previewHeight = previewRect.Height;
			double previewLeft = previewRect.Left, previewTop = previewRect.Top;

			// Transform the left and top of the tap to account for rotation
			switch (displayOrientation)
			{
				case DisplayOrientations.Portrait:
					var tempLeft = left;

					left = top;
					top = previewRect.Width - tempLeft;
					break;
				case DisplayOrientations.LandscapeFlipped:
					left = previewRect.Width - left;
					top = previewRect.Height - top;
					break;
				case DisplayOrientations.PortraitFlipped:
					var tempTop = top;

					top = left;
					left = previewRect.Width - tempTop;
					break;
			}

			// For portrait orientations, the information about the active preview area needs to be rotated
			if (displayOrientation == DisplayOrientations.Portrait || displayOrientation == DisplayOrientations.PortraitFlipped)
			{
				previewWidth = previewRect.Height;
				previewHeight = previewRect.Width;
				previewLeft = previewRect.Top;
				previewTop = previewRect.Left;
			}

			// Normalize width and height of the focus rectangle
			var width = size.Width / previewWidth;
			var height = size.Height / previewHeight;

			// Shift rect left and top to be relative to just the active preview area
			left -= previewLeft;
			top -= previewTop;

			// Normalize left and top
			left /= previewWidth;
			top /= previewHeight;

			// Ensure rectangle is fully contained within the active preview area horizontally
			left = Math.Max(left, 0);
			left = Math.Min(1 - width, left);

			// Ensure rectangle is fully contained within the active preview area vertically
			top = Math.Max(top, 0);
			top = Math.Min(1 - height, top);

			// Create and return resulting rectangle
			return new Rect(left, top, width, height);
		}

		/// <summary>
		/// Calculates the size and location of the rectangle that contains the preview stream within the preview control, when the scaling mode is Uniform
		/// </summary>
		/// <param name="previewResolution">The resolution at which the preview is running</param>
		/// <param name="previewControl">The control that is displaying the preview using Uniform as the scaling mode</param>
		/// <returns></returns>
		static Rect GetPreviewStreamRectInControl(Size previewResolution, CaptureElement previewControl)
		{
			var result = new Rect();

			// In case this function is called before everything is initialized correctly, return an empty result
			if (previewControl == null || previewControl.ActualHeight < 1 || previewControl.ActualWidth < 1 ||
				previewResolution.Height < 1 || previewResolution.Width < 1)
			{
				return result;
			}

			var streamWidth = previewResolution.Width;
			var streamHeight = previewResolution.Height;

			// Start by assuming the preview display area in the control spans the entire width and height both (this is corrected in the next if for the necessary dimension)
			result.Width = previewControl.ActualWidth;
			result.Height = previewControl.ActualHeight;

			// If UI is "wider" than preview, letterboxing will be on the sides
			if ((previewControl.ActualWidth / previewControl.ActualHeight > streamWidth / (double)streamHeight))
			{
				var scale = previewControl.ActualHeight / streamHeight;
				var scaledWidth = streamWidth * scale;

				result.X = (previewControl.ActualWidth - scaledWidth) / 2.0;
				result.Width = scaledWidth;
			}
			else // Preview stream is "wider" than UI, so letterboxing will be on the top+bottom
			{
				var scale = previewControl.ActualWidth / streamWidth;
				var scaledHeight = streamHeight * scale;

				result.Y = (previewControl.ActualHeight - scaledHeight) / 2.0;
				result.Height = scaledHeight;
			}

			return result;
		}
	}
}
