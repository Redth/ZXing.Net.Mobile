using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ZXing.Net.Mobile
{
    public sealed partial class DefaultOverlayControl : UserControl
    {
        public DefaultOverlayControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TopTextProperty = DependencyProperty.Register(
            "TopText", typeof(string), typeof(DefaultOverlayControl), new PropertyMetadata(default(string)));

        public string TopText
        {
            get { return (string) GetValue(TopTextProperty); }
            set { SetValue(TopTextProperty, value); }
        }

        public static readonly DependencyProperty BottomTextProperty = DependencyProperty.Register(
            "BottomText", typeof(string), typeof(DefaultOverlayControl), new PropertyMetadata(default(string)));

        public string BottomText
        {
            get { return (string) GetValue(BottomTextProperty); }
            set { SetValue(BottomTextProperty, value); }
        }

        public static readonly DependencyProperty CancelButtonTextProperty = DependencyProperty.Register(
            "CancelButtonText", typeof(string), typeof(DefaultOverlayControl), new PropertyMetadata(default(string)));

        public string CancelButtonText
        {
            get { return (string) GetValue(CancelButtonTextProperty); }
            set { SetValue(CancelButtonTextProperty, value); }
        }

        public static readonly DependencyProperty FlashButtonTextProperty = DependencyProperty.Register(
            "FlashButtonText", typeof(string), typeof(DefaultOverlayControl), new PropertyMetadata(default(string)));

        public string FlashButtonText
        {
            get { return (string) GetValue(FlashButtonTextProperty); }
            set { SetValue(FlashButtonTextProperty, value); }
        }

        public static readonly DependencyProperty CancelButtonCommandProperty = DependencyProperty.Register(
            "CancelButtonCommand", typeof(ICommand), typeof(DefaultOverlayControl), new PropertyMetadata(default(ICommand)));

        public ICommand CancelButtonCommand
        {
            get { return (ICommand) GetValue(CancelButtonCommandProperty); }
            set { SetValue(CancelButtonCommandProperty, value); }
        }

        public static readonly DependencyProperty FlashButtonCommandProperty = DependencyProperty.Register(
            "FlashButtonCommand", typeof(ICommand), typeof(DefaultOverlayControl), new PropertyMetadata(default(ICommand)));

        public ICommand FlashButtonCommand
        {
            get { return (ICommand) GetValue(FlashButtonCommandProperty); }
            set { SetValue(FlashButtonCommandProperty, value); }
        }
    }
}
