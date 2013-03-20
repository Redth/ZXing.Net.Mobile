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
			this.DelayBetweenAnalyzingFrames = 150;
			this.InitialDelayBeforeAnalyzingFrames = 300;
		}

		public List<BarcodeFormat> PossibleFormats { get;set; }
		public bool? TryHarder { get;set; }
		public bool? PureBarcode { get;set; }
		public bool? AutoRotate { get;set; }
		public string CharacterSet { get;set; }

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
				reader.TryHarder = this.TryHarder.Value;
			if (this.PureBarcode.HasValue)
				reader.PureBarcode = this.PureBarcode.Value;
			if (this.AutoRotate.HasValue)
				reader.AutoRotate = this.AutoRotate.Value;
			if (!string.IsNullOrEmpty (this.CharacterSet))
				reader.CharacterSet = this.CharacterSet;

			if (this.PossibleFormats != null && this.PossibleFormats.Count > 0)
			{
				reader.PossibleFormats = new List<BarcodeFormat>();

				foreach (var pf in this.PossibleFormats)
					reader.PossibleFormats.Add(pf);
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
			//if (this.AutoRotate.HasValue && this.AutoRotate.Value)

			if (this.PossibleFormats != null && this.PossibleFormats.Count > 0)
			hints.Add(DecodeHintType.POSSIBLE_FORMATS, this.PossibleFormats);

			reader.Hints = hints;

			return reader;
		}
	}
}

