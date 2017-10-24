using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace FormsSample.iOS
{
    [Register ("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        App formsApp;

        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init ();
                       
            global::ZXing.Net.Mobile.Forms.iOS.Platform.Init ();

            // Enable test cloud
            Xamarin.Calabash.Start();

            formsApp = new App ();

            LoadApplication (formsApp);

            return base.FinishedLaunching (app, options);
        }

        [Export ("UITestBackdoorScan:")] // notice the colon at the end of the method name
        public NSString UITestBackdoorScan (NSString value)
        {
            formsApp.UITestBackdoorScan (value.ToString ());
            return new NSString ();
        }
    }
}

