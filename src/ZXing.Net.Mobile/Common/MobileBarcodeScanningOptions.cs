using System;
using System.Collections;
using System.Collections.Generic;
using ZXing;

namespace ZXing.Mobile
{
	public class MobileBarcodeScanningOptions
	{
		public MobileBarcodeScanningOptions ()
		{
			this.PossibleFormats = new List<BarcodeFormat>();
			//this.AutoRotate = true;
			this.DelayBetweenAnalyzingFrames = 150;
			this.InitialDelayBeforeAnalyzingFrames = 300;
		}

		public List<BarcodeFormat> PossibleFormats { get;set; }
		public bool? TryHarder { get;set; } 
		public bool? PureBarcode { get;set; }
		public bool? AutoRotate { get;set; }
		public string CharacterSet { get;set; }
		public bool? TryInverted { get;set; }
		public bool? UseFrontCameraIfAvailable { get; set; }

		public int DelayBetweenAnalyzingFrames { get;set;}
		public int InitialDelayBeforeAnalyzingFrames { get;set; }

		/// <summary>
		/// If a function is provided here, it is used to customize the preview
		/// size of the camera. The function can return null to use the default.
		/// </summary>
		/// <remarks>
		/// Selecting a lower preview size improves performance on some devices.
		/// E.g., a 2013 Nexus 7 defaults to 1920 x 1080, the processing of which
		/// seems to cause significant lag.
		/// </remarks>
		public Func<IList<Dimension>, Dimension> PreviewSizeSelector { get;set; }

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
	}
}

