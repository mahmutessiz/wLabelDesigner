using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using wLabelDesigner.Services;

namespace wLabelDesigner.Converters;

public sealed class ColorStringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        LabelDrawingRenderer.CreateBrush(value as string, Brushes.Transparent);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
