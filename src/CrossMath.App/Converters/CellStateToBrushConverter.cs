using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CrossMath.App.ViewModels;

namespace CrossMath.App.Converters;

[ValueConversion(typeof(CellState), typeof(Brush))]
public sealed class CellStateToBrushConverter : IValueConverter
{
    public Brush Normal { get; set; } = Brushes.White;
    public Brush Correct { get; set; } = Brushes.LightGreen;
    public Brush Wrong { get; set; } = Brushes.LightPink;
    public Brush Hinted { get; set; } = Brushes.LightBlue;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        CellState.Correct => Correct,
        CellState.Wrong => Wrong,
        CellState.Hinted => Hinted,
        _ => Normal,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
