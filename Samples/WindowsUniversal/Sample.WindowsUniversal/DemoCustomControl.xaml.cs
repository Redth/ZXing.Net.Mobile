using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Sample.WindowsUniversal
{
    public sealed partial class DemoCustomControl : UserControl
    {
        public DemoCustomControl()
        {
            InitializeComponent();
        }

        public event EventHandler ButtonCancelClicked;
        public event EventHandler ButtonToggleTorchClicked;

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            ButtonCancelClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnTorchButtonClick(object sender, RoutedEventArgs e)
        {
            ButtonToggleTorchClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
