using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ZXing.Net.Mobile
{
    public class StringContentToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var str = value as string;
            return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}