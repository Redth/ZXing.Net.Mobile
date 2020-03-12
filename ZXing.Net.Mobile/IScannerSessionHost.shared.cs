using System;
namespace ZXing.Mobile
{
	public interface IScannerSessionHost
	{
		ZXing.Mobile.MobileBarcodeScanningOptions ScanningOptions { get; }
	}
}
