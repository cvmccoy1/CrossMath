using System.Globalization;
using System.Windows.Data;

namespace CrossMath.App.Converters;

[ValueConversion(typeof(TimeSpan), typeof(string))]
public sealed class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is TimeSpan t
            ? t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss", culture) : t.ToString(@"mm\:ss", culture)
            : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
