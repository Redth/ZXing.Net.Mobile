using System;
using Xamarin.Forms;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingDefaultOverlay : Grid
    {
        Label topText;
        Label botText;
        Button flash;

        public delegate void FlashButtonClickedDelegate (Button sender, EventArgs e);
        public event FlashButtonClickedDelegate FlashButtonClicked;
           
        public ZXingDefaultOverlay ()
        {
            VerticalOptions = LayoutOptions.FillAndExpand;
            HorizontalOptions = LayoutOptions.FillAndExpand;

            RowDefinitions.Add (new RowDefinition { Height = new GridLength (1, GridUnitType.Star) });
            RowDefinitions.Add (new RowDefinition { Height = new GridLength (2, GridUnitType.Star) });
            RowDefinitions.Add (new RowDefinition { Height = new GridLength (1, GridUnitType.Star) });
            ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (1, GridUnitType.Star) });


            Children.Add (new BoxView {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                BackgroundColor = Color.Black,
                Opacity = 0.7,
            }, 0, 0);

            Children.Add (new BoxView {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                BackgroundColor = Color.Black,
                Opacity = 0.7,
            }, 0, 2);

            Children.Add (new BoxView {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = 3,
                BackgroundColor = Color.Red,
                Opacity = 0.6
            }, 0, 1);

            topText = new Label {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.White
            };
            Children.Add (topText, 0, 0);

            botText = new Label {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.White
            };
            Children.Add (botText, 0, 2);

            flash = new Button {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Text = "Flash",
                TextColor = Color.White,
                IsVisible = false
            };
            flash.Clicked += (sender, e) => {
                FlashButtonClicked?.Invoke(flash, e);
            };

            Children.Add (flash, 0, 0);
        }

        // Jason Couture - jcouture AT pssproducts.com
        // Make these properties bindable, and proxy them to the target objects instead.
        // Making this overlay far more useful for MVVM, where we really want to avoid code behind the view (Except in the view model)
        // while still retaining its usefulness elsewhere.


        // New Property: FlashCommand, Proxied to flash.Command
        public static BindableProperty FlashCommandProperty = BindableProperty.Create(nameof(FlashCommand), typeof(Command), typeof(ZXingDefaultOverlay), null, propertyChanged: OnFlashCommandChanged);
        public Command FlashCommand { get { return GetValue(FlashCommandProperty) as Command; } set { SetValue(FlashCommandProperty, value); } }
        private static void OnFlashCommandChanged(BindableObject bindable, object oldvalue, object newValue)
        {
            var overlay = bindable as ZXingDefaultOverlay;
            if (overlay?.flash == null) return;
            overlay.flash.Command = newValue as Command;
        }


        // Updated property: TopText, was TopText <-> topText.Text
        public static BindableProperty TopTextProperty = BindableProperty.Create(nameof(TopText), typeof(string), typeof(ZXingDefaultOverlay), null, propertyChanged: OnTopTextChanged);
        public string TopText { get { return GetValue(TopTextProperty) as string; } set { SetValue(TopTextProperty, value); } }
        private static void OnTopTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SetLabelText((bindable as ZXingDefaultOverlay)?.topText, newValue);
        }

        // Updated property: TopText, was BottomText <-> botText.Text
        public static BindableProperty BottomTextProperty = BindableProperty.Create(nameof(BottomText), typeof(string), typeof(ZXingDefaultOverlay), null, propertyChanged: OnBottomTextChanged);
        public string BottomText { get { return GetValue(BottomTextProperty) as string; } set { SetValue(BottomTextProperty, value); } }
        private static void OnBottomTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SetLabelText((bindable as ZXingDefaultOverlay)?.botText, newValue);
        }

        private static void SetLabelText(Label label, object newValue)
        {
            if (label == null) return;
            label.Text = newValue as string; // Should this be ?? ""; ?
        }

        // Updated property: ShowFlashButton, was ShowFlashButton <-> flash.IsVisible
        public static BindableProperty ShowFlashButtonProperty = BindableProperty.Create(nameof(ShowFlashButton), typeof(bool), typeof(ZXingDefaultOverlay), false, propertyChanged: OnFlashButtonVisibleChanged);
        public bool ShowFlashButton { get { return (bool)GetValue(ShowFlashButtonProperty); } set { SetValue(ShowFlashButtonProperty, value); } }
        private static void OnFlashButtonVisibleChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            var overlay = bindable as ZXingDefaultOverlay;
            if (overlay?.flash == null) return;
            overlay.flash.IsVisible = overlay.ShowFlashButton;
        }
    }
}

