using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using wLabelDesigner.Models;

namespace wLabelDesigner.Converters;

public sealed class StrokeStyleToDashArrayConverter : IValueConverter
{
    private static readonly DoubleCollection Solid = [];
    private static readonly DoubleCollection Dashed = [4, 2];
    private static readonly DoubleCollection Dotted = [0, 2];

    static StrokeStyleToDashArrayConverter()
    {
        Solid.Freeze();
        Dashed.Freeze();
        Dotted.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is StrokeStyleOption style
            ? style switch
            {
                StrokeStyleOption.Dashed => Dashed,
                StrokeStyleOption.Dotted => Dotted,
                _ => Solid
            }
            : Solid;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
