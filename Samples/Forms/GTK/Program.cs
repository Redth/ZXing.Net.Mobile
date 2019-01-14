using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;
using Application = Gtk.Application;

namespace FormsSample.GTK
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Application.Init();
            Forms.Init();
            ZXing.Net.Mobile.Forms.GTK.Platform.Init ();

            var app = new App();
            var window = new FormsWindow();
            window.LoadApplication(app);
            window.SetApplicationTitle("GTK example");
            window.Show();
            Application.Run();
        }
    }
}
