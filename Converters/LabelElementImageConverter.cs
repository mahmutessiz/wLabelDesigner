using System.Globalization;
using System.Windows.Data;
using wLabelDesigner.Models;
using wLabelDesigner.Services;

namespace wLabelDesigner.Converters;

public sealed class LabelElementImageConverter : IMultiValueConverter
{
    public object? Convert(
        object[] values,
        System.Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string content || string.IsNullOrWhiteSpace(content) ||
            values[1] is not LabelElementKind kind)
        {
            return null;
        }

        try
        {
            return LabelImageRenderer.Render(kind, content);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    public object[] ConvertBack(
        object value,
        System.Type[] targetTypes,
        object parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();

}
