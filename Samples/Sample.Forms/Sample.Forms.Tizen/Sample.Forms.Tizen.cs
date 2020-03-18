using System;
using Sample.Forms;
using Tizen.Security;

namespace FormsSample.Tizen
{
	class Program : global::Xamarin.Forms.Platform.Tizen.FormsApplication
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			LoadApplication(new App());

			var cameraPermission = "http://tizen.org/privilege/camera";
			PrivacyPrivilegeManager.RequestPermission(cameraPermission);
		}

		static void Main(string[] args)
		{
			var app = new Program();

			Xamarin.Forms.Forms.Init(app, true);

			ZXing.Net.Mobile.Forms.Tizen.Platform.Init();

			app.Run(args);
		}
	}
}
