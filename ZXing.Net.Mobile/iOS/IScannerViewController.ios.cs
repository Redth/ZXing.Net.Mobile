using System;

using UIKit;

namespace ZXing.Mobile
{
	public interface IScannerViewController : IScannerView
	{
		void Cancel();
	}
}

