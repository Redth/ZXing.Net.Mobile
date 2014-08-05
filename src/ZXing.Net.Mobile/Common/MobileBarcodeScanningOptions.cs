using System;
using System.Collections;
using System.Collections.Generic;
using ZXing;

namespace ZXing.Mobile
{
	public class MobileBarcodeScanningOptions
	{
		/// <summary>
		/// Camera resolution selector delegate, must return the selected Resolution from the list of available resolutions
		/// </summary>
		public delegate CameraResolution CameraResolutionSelectorDelegate (List<CameraResolution> availableResolutions);

		public MobileBarcodeScanningOptions ()
		{
			this.PossibleFormats = new List<BarcodeFormat>();
			//this.AutoRotate = true;
			this.DelayBetweenAnalyzingFrames = 150;
			this.InitialDelayBeforeAnalyzingFrames = 300;
		}

		public CameraResolutionSelectorDelegate CameraResolutionSelector { get;set; }
		public List<BarcodeFormat> PossibleFormats { get;set; }
		public bool? TryHarder { get;set; } 
		public bool? PureBarcode { get;set; }
		public bool? AutoRotate { get;set; }
		public string CharacterSet { get;set; }
		public bool? TryInverted { get;set; }
		public bool? UseFrontCameraIfAvailable { get; set; }

		public int DelayBetweenAnalyzingFrames { get;set;}
		public int InitialDelayBeforeAnalyzingFrames { get;set; }

		public static MobileBarcodeScanningOptions Default
		{
			get { return new MobileBarcodeScanningOptions(); }
		}

		public BarcodeReader BuildBarcodeReader ()
		{
			var reader = new BarcodeReader ();
			if (this.TryHarder.HasValue)
				reader.Options.TryHarder = this.TryHarder.Value;
			if (this.PureBarcode.HasValue)
				reader.Options.PureBarcode = this.PureBarcode.Value;
			if (this.AutoRotate.HasValue)
				reader.AutoRotate = this.AutoRotate.Value;
			if (!string.IsNullOrEmpty (this.CharacterSet))
				reader.Options.CharacterSet = this.CharacterSet;
			if (this.TryInverted.HasValue)
				reader.TryInverted = this.TryInverted.Value;

			if (this.PossibleFormats != null && this.PossibleFormats.Count > 0)
			{
				reader.Options.PossibleFormats = new List<BarcodeFormat>();

				foreach (var pf in this.PossibleFormats)
					reader.Options.PossibleFormats.Add(pf);
			}

			return reader;
		}

		public MultiFormatReader BuildMultiFormatReader()
		{
			var reader = new MultiFormatReader();

			var hints = new Dictionary<DecodeHintType, object>();

			if (this.TryHarder.HasValue && this.TryHarder.Value)
				hints.Add(DecodeHintType.TRY_HARDER, this.TryHarder.Value);
			if (this.PureBarcode.HasValue && this.PureBarcode.Value)
				hints.Add(DecodeHintType.PURE_BARCODE, this.PureBarcode.Value);

			if (this.PossibleFormats != null && this.PossibleFormats.Count > 0)
			hints.Add(DecodeHintType.POSSIBLE_FORMATS, this.PossibleFormats);

			reader.Hints = hints;

			return reader;
		}

		internal CameraResolution GetResolution (List<CameraResolution> availableResolutions)
		{
			CameraResolution r = null;

			var dg = CameraResolutionSelector;

			if (dg != null) {
				r = dg (availableResolutions);
			}

			return r;
		}
	}
}

