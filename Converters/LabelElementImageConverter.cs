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
            var barcodeFormat = values.Length > 2 && values[2] is BarcodeFormatOption format
                ? format
                : BarcodeFormatOption.Code128;
            return LabelImageRenderer.Render(kind, content, barcodeFormat);
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
