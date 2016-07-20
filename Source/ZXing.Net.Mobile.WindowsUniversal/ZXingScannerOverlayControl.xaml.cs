using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ZXing.Mobile
{
    public sealed partial class ZXingScannerOverlayControl : UserControl
    {
        public ZXingScannerOverlayControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            MiddleLineAnimation.Begin();
        }

        public static readonly DependencyProperty TopTextProperty = DependencyProperty.Register(
            nameof(TopText), typeof(string), typeof(ZXingScannerOverlayControl), new PropertyMetadata(default(string)));

        public string TopText
        {
            get { return (string) GetValue(TopTextProperty); }
            set { SetValue(TopTextProperty, value); }
        }

        public static readonly DependencyProperty BottomTextProperty = DependencyProperty.Register(
            nameof(BottomText), typeof(string), typeof(ZXingScannerOverlayControl), new PropertyMetadata(default(string)));

        public string BottomText
        {
            get { return (string) GetValue(BottomTextProperty); }
            set { SetValue(BottomTextProperty, value); }
        }
    }
}
