using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZXing;

namespace ZXing.UI
{
	public class BarcodeScannerSettings
	{
		/// <summary>
		/// Camera resolution selector delegate, must return the selected Resolution from the list of available resolutions
		/// </summary>
		public delegate CameraResolution CameraResolutionSelectorDelegate(List<CameraResolution> availableResolutions);

		public BarcodeScannerSettings()
			: this(null)
		{
		}

		public BarcodeScannerSettings(Common.DecodingOptions decodingOptions)
		{
			DecodingOptions = decodingOptions ?? new Common.DecodingOptions();

			DelayBetweenAnalyzingFrames = TimeSpan.FromMilliseconds(150);
			InitialDelayBeforeAnalyzingFrames = TimeSpan.FromMilliseconds(300);
			DelayBetweenContinuousScans = TimeSpan.FromSeconds(1);
			UseNativeScanning = false;
		}

		public readonly Common.DecodingOptions DecodingOptions;

		public CameraResolutionSelectorDelegate CameraResolutionSelector { get; set; }

		public bool? AutoRotate { get; set; }

		public bool? TryInverted { get; set; }

		public bool? UseFrontCameraIfAvailable { get; set; }

		public bool DisableAutofocus { get; set; } = false;

		public bool UseNativeScanning { get; set; } = false;

		public TimeSpan DelayBetweenContinuousScans { get; set; } = TimeSpan.Zero;

		public TimeSpan DelayBetweenAnalyzingFrames { get; set; } = TimeSpan.Zero;

		public TimeSpan InitialDelayBeforeAnalyzingFrames { get; set; } = TimeSpan.Zero;

		public bool DecodeMultipleBarcodes { get; set; } = false;

		public BarcodeReader BuildBarcodeReader()
		{
			var reader = new BarcodeReader
			{
				Options = DecodingOptions
			};

			if (AutoRotate.HasValue)
				reader.AutoRotate = AutoRotate.Value;
			if (TryInverted.HasValue)
				reader.TryInverted = TryInverted.Value;

			return reader;
		}

		public CameraResolution GetResolution(List<CameraResolution> availableResolutions)
		{
			CameraResolution r = null;

			var dg = CameraResolutionSelector;

			if (dg != null)
				r = dg(availableResolutions);

			return r;
		}
	}
}
