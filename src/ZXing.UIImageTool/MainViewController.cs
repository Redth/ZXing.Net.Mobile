using System;
using MonoTouch.UIKit;

namespace ZXing.UIImageTool
{
	public class MainViewController : UIViewController 
	{
		public MainViewController ()
		{
		}

		CaptureViewController vc;

		public override void ViewDidLoad ()
		{

			var bbItem = new UIBarButtonItem(UIBarButtonSystemItem.Play);
			bbItem.Clicked += (object sender, EventArgs e) => {
				vc = new CaptureViewController();

				vc.Done += () => {
					this.InvokeOnMainThread(() => this.NavigationController.DismissViewController(true, null));
				};

				this.InvokeOnMainThread(() => this.NavigationController.PresentViewController(vc, true, null));

			};

			this.NavigationItem.RightBarButtonItem = bbItem;

		}
	}
}

