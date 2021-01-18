using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZXing;

namespace ZXing.Mobile
{
	public class MobileBarcodeScanningOptions
	{
		/// <summary>
		/// Camera resolution selector delegate, must return the selected Resolution from the list of available resolutions
		/// </summary>
		public delegate CameraResolution CameraResolutionSelectorDelegate(List<CameraResolution> availableResolutions);

		public MobileBarcodeScanningOptions()
		{
			PossibleFormats = new List<BarcodeFormat>();
			//this.AutoRotate = true;
			DelayBetweenAnalyzingFrames = 150;
			InitialDelayBeforeAnalyzingFrames = 300;
			DelayBetweenContinuousScans = 1000;
			UseNativeScanning = false;
		}

		public CameraResolutionSelectorDelegate CameraResolutionSelector { get; set; }

		public IEnumerable<BarcodeFormat> PossibleFormats { get; set; }

		public bool? TryHarder { get; set; }

		public bool? PureBarcode { get; set; }

		public bool? AutoRotate { get; set; }

		public bool? UseCode39ExtendedMode { get; set; }

		public string CharacterSet { get; set; }

		public bool? TryInverted { get; set; }

		public bool? UseFrontCameraIfAvailable { get; set; }

		public bool? AssumeGS1 { get; set; }


		public bool DisableAutofocus { get; set; }

		public bool UseNativeScanning { get; set; }

		public int DelayBetweenContinuousScans { get; set; }

		public int DelayBetweenAnalyzingFrames { get; set; }
		public int InitialDelayBeforeAnalyzingFrames { get; set; }

		public static MobileBarcodeScanningOptions Default
		{
			get { return new MobileBarcodeScanningOptions(); }
		}

		public BarcodeReaderGeneric BuildBarcodeReader()
		{
			var reader = new BarcodeReaderGeneric();
			if (TryHarder.HasValue)
				reader.Options.TryHarder = TryHarder.Value;
			if (PureBarcode.HasValue)
				reader.Options.PureBarcode = PureBarcode.Value;
			if (AutoRotate.HasValue)
				reader.AutoRotate = AutoRotate.Value;
			if (UseCode39ExtendedMode.HasValue)
				reader.Options.UseCode39ExtendedMode = UseCode39ExtendedMode.Value;
			if (!string.IsNullOrEmpty(CharacterSet))
				reader.Options.CharacterSet = CharacterSet;
			if (TryInverted.HasValue)
				reader.TryInverted = TryInverted.Value;
			if (AssumeGS1.HasValue)
				reader.Options.AssumeGS1 = AssumeGS1.Value;

			if (PossibleFormats?.Any() ?? false)
			{
				reader.Options.PossibleFormats = new List<BarcodeFormat>();

				foreach (var pf in PossibleFormats)
					reader.Options.PossibleFormats.Add(pf);
			}

			return reader;
		}

		public MultiFormatReader BuildMultiFormatReader()
		{
			var reader = new MultiFormatReader();

			var hints = new Dictionary<DecodeHintType, object>();

			if (TryHarder.HasValue && TryHarder.Value)
				hints.Add(DecodeHintType.TRY_HARDER, TryHarder.Value);
			if (PureBarcode.HasValue && PureBarcode.Value)
				hints.Add(DecodeHintType.PURE_BARCODE, PureBarcode.Value);

			if (PossibleFormats?.Any() ?? false)
				hints.Add(DecodeHintType.POSSIBLE_FORMATS, PossibleFormats);

			reader.Hints = hints;

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
