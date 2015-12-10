using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WindowsPhone.Resources;
using Xamarin.Forms;

namespace WindowsPhone
{
    public partial class MainPage : PhoneApplicationPage
    {
        FormsSample.App app;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Force the renderer assembly to load
            ZXing.Net.Mobile.Forms.WindowsPhone.ZXingScannerViewRenderer.Init();

            Forms.Init();

            app = new FormsSample.App();
            // Set our view from the "main" layout resource
            Content = app.MainPage.ConvertPageToUIElement(this);
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}