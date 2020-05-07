using System;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using CoreFoundation;
using UIKit;

namespace ZXing.Mobile
{
	public partial class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		IScannerViewController viewController;
		readonly WeakReference<UIViewController> weakAppController;
		readonly ManualResetEvent scanResultResetEvent = new ManualResetEvent(false);

		public MobileBarcodeScanner(UIViewController delegateController)
			=> weakAppController = new WeakReference<UIViewController>(delegateController);

		public MobileBarcodeScanner()
			=> weakAppController = new WeakReference<UIViewController>(Xamarin.Essentials.Platform.GetCurrentUIViewController());

		public Task<Result> Scan(bool useAVCaptureEngine)
			=> Scan(new MobileBarcodeScanningOptions(), useAVCaptureEngine);


		Task<Result> PlatformScan(MobileBarcodeScanningOptions options)
			=> Scan(options, false);

		void PlatformScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
			=> InternalScanContinuously(options, false, scanHandler);

		public void ScanContinuously(MobileBarcodeScanningOptions options, bool useAVCaptureEngine, Action<Result> scanHandler)
			=> InternalScanContinuously(options, useAVCaptureEngine, scanHandler);

		void InternalScanContinuously(MobileBarcodeScanningOptions options, bool useAVCaptureEngine, Action<Result> scanHandler)
		{
			try
			{
				var sv = new Version(0, 0, 0);
				Version.TryParse(UIDevice.CurrentDevice.SystemVersion, out sv);

				var is7orgreater = sv.Major >= 7;
				var allRequestedFormatsSupported = true;

				if (useAVCaptureEngine)
					allRequestedFormatsSupported = AVCaptureScannerView.SupportsAllRequestedBarcodeFormats(options.PossibleFormats);

				if (weakAppController.TryGetTarget(out var appController))
				{
					var tcs = new TaskCompletionSource<object>();

					appController?.InvokeOnMainThread(() =>
					{
						if (useAVCaptureEngine && is7orgreater && allRequestedFormatsSupported)
						{
							viewController = new AVCaptureScannerViewController(options, this);
							viewController.ContinuousScanning = true;
						}
						else
						{
							if (useAVCaptureEngine && !is7orgreater)
								Console.WriteLine("Not iOS 7 or greater, cannot use AVCapture for barcode decoding, using ZXing instead");
							else if (useAVCaptureEngine && !allRequestedFormatsSupported)
								Console.WriteLine("Not all requested barcode formats were supported by AVCapture, using ZXing instead");

							viewController = new ZXing.Mobile.ZXingScannerViewController(options, this);
							viewController.ContinuousScanning = true;
						}

						viewController.OnScannedResult += barcodeResult =>
						{
							// If null, stop scanning was called
							if (barcodeResult == null)
							{
								((UIViewController)viewController).InvokeOnMainThread(() =>
								{
									((UIViewController)viewController).DismissViewController(true, null);
								});
							}

							scanHandler(barcodeResult);
						};

						appController?.PresentViewController((UIViewController)viewController, true, null);
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public Task<Result> Scan(MobileBarcodeScanningOptions options, bool useAVCaptureEngine) => Task.Factory.StartNew(() =>
		{
			try
			{
				scanResultResetEvent.Reset();

				Result result = null;

				var sv = new Version(0, 0, 0);
				Version.TryParse(UIDevice.CurrentDevice.SystemVersion, out sv);

				var is7orgreater = sv.Major >= 7;
				var allRequestedFormatsSupported = true;

				if (useAVCaptureEngine)
					allRequestedFormatsSupported = AVCaptureScannerView.SupportsAllRequestedBarcodeFormats(options.PossibleFormats);

				if (weakAppController.TryGetTarget(out var appController))
				{
					appController?.InvokeOnMainThread(() =>
					{
						if (useAVCaptureEngine && is7orgreater && allRequestedFormatsSupported)
						{
							viewController = new AVCaptureScannerViewController(options, this);
						}
						else
						{
							if (useAVCaptureEngine && !is7orgreater)
								Console.WriteLine("Not iOS 7 or greater, cannot use AVCapture for barcode decoding, using ZXing instead");
							else if (useAVCaptureEngine && !allRequestedFormatsSupported)
								Console.WriteLine("Not all requested barcode formats were supported by AVCapture, using ZXing instead");

							viewController = new ZXing.Mobile.ZXingScannerViewController(options, this);
						}

						viewController.OnScannedResult += barcodeResult =>
						{

							((UIViewController)viewController).InvokeOnMainThread(() =>
							{

								viewController.Cancel();

								// Handle error situation that occurs when user manually closes scanner in the same moment that a QR code is detected
								try
								{
									((UIViewController)viewController).DismissViewController(true, () =>
									{
										result = barcodeResult;
										scanResultResetEvent.Set();
									});
								}
								catch (ObjectDisposedException)
								{
									// In all likelihood, iOS has decided to close the scanner at this point. But just in case it executes the
									// post-scan code instead, set the result so we will not get a NullReferenceException.
									result = barcodeResult;
									scanResultResetEvent.Set();
								}
							});
						};

						appController?.PresentViewController((UIViewController)viewController, true, null);
					});
				}

				scanResultResetEvent.WaitOne();
				((UIViewController)viewController).Dispose();

				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return null;
			}
		});

		void PlatformCancel()
		{
			if (viewController != null)
			{
				((UIViewController)viewController).InvokeOnMainThread(() =>
				{
					viewController.Cancel();

					// Calling with animated:true here will result in a blank screen when the scanner is closed on iOS 7.
					((UIViewController)viewController).DismissViewController(true, null);
				});
			}

			scanResultResetEvent.Set();
		}

		void PlatformTorch(bool on)
			=> viewController?.Torch(on);

		void PlatformToggleTorch()
			=> viewController?.ToggleTorch();

		void PlatformAutoFocus()
		{
			//Does nothing on iOS
		}

		void PlatformPauseAnalysis()
			=> viewController?.PauseAnalysis();

		void PlatformResumeAnalysis()
			=> viewController?.ResumeAnalysis();

		bool PlatformIsTorchOn
			=> viewController.IsTorchOn;

		public UIView CustomOverlay { get; set; }
	}
}
