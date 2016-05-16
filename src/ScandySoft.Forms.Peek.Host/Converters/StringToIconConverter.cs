using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace ScandySoft.Forms.Peek.Host.Converters
{
    [ValueConversion(typeof(string), typeof(PackIconKind))]
    public class StringToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (string)value;
            var result = (PackIconKind) Enum.Parse(typeof(PackIconKind), stringValue);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
