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
                var h = FlashButtonClicked;
                if (h != null)
                    h(flash, e);
            };

            Children.Add (flash, 0, 0);
        }

        public string TopText {  get { return topText.Text; } set { topText.Text = value; } }
        public string BottomText {  get { return botText.Text; } set { botText.Text = value; } }

        public bool ShowFlashButton { 
            get { 
                return flash.IsVisible;
            }
            set { 
                flash.IsVisible = value;
            }
        }
    }
}

