using System;
using System.Windows.Input;
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
			BindingContext = this;

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
                Opacity = 0.6,
            }, 0, 1);

            topText = new Label {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.White,
                AutomationId = "zxingDefaultOverlay_TopTextLabel",
            };
            topText.SetBinding( Label.TextProperty, new Binding( nameof( TopText ) ) );
            Children.Add (topText, 0, 0);

            botText = new Label {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.White,
                AutomationId = "zxingDefaultOverlay_BottomTextLabel",
            };
            botText.SetBinding( Label.TextProperty, new Binding( nameof( BottomText ) ) );
            Children.Add (botText, 0, 2);

            flash = new Button {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Text = "Flash",
                TextColor = Color.White,
                AutomationId = "zxingDefaultOverlay_FlashButton",
            };
            flash.SetBinding( Button.IsVisibleProperty, new Binding( nameof( ShowFlashButton ) ) );
            flash.Clicked += (sender, e) => {
                FlashButtonClicked?.Invoke( flash, e );
            };

            Children.Add (flash, 0, 0);
        }

        public static readonly BindableProperty TopTextProperty =
            BindableProperty.Create( nameof( TopText ), typeof( string ), typeof( ZXingDefaultOverlay ), string.Empty );
        public string TopText
        {
            get { return ( string )GetValue( TopTextProperty ); }
            set { SetValue( TopTextProperty, value ); }
        }

        public static readonly BindableProperty BottomTextProperty =
            BindableProperty.Create( nameof( BottomText ), typeof( string ), typeof( ZXingDefaultOverlay ), string.Empty );
        public string BottomText
        {
            get { return ( string )GetValue( BottomTextProperty ); }
            set { SetValue( BottomTextProperty, value ); }
        }

        public static readonly BindableProperty ShowFlashButtonProperty =
            BindableProperty.Create( nameof( ShowFlashButton ), typeof( bool ), typeof( ZXingDefaultOverlay ), false );
        public bool ShowFlashButton
        { 
            get { return ( bool )GetValue( ShowFlashButtonProperty ); }
            set { SetValue( ShowFlashButtonProperty, value ); }
        }

        public static BindableProperty FlashCommandProperty = 
            BindableProperty.Create( nameof( FlashCommand ), typeof( ICommand ), typeof( ZXingDefaultOverlay ), 
                defaultValue: default(ICommand), 
                propertyChanged: OnFlashCommandChanged );
        public ICommand FlashCommand
        {
            get { return (ICommand)GetValue( FlashCommandProperty ); }
            set { SetValue( FlashCommandProperty, value ); }
        }

        private static void OnFlashCommandChanged( BindableObject bindable, object oldvalue, object newValue )
        {
            var overlay = bindable as ZXingDefaultOverlay;
            if( overlay?.flash == null ) return;
            overlay.flash.Command = newValue as Command;
        }
    }
}

