using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FormsSample
{
    public class HomePage : ContentPage
    {
        Button buttonScan;

        public HomePage () : base ()
        {
            buttonScan = new Button
            {
                Text = "Scan with Default Overlay",
            };
            buttonScan.Clicked += async delegate
            {
                await Navigation.PushAsync(new ScanPage());
            };

            var stack = new StackLayout
            {

            };

            stack.Children.Add(buttonScan);

            Content = stack;
        }
    }
}
