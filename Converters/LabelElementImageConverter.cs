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
            var isBarcodeTextVisible = values.Length <= 3 || values[3] is not bool isVisible || isVisible;
            var quietZoneMillimeters = values.Length > 4 && values[4] is double quietZone ? quietZone : 2;
            var elementWidthMillimeters = values.Length > 5 && values[5] is double width ? width : 50;
            var qrErrorCorrection = values.Length > 6 && values[6] is QrErrorCorrectionOption correction
                ? correction
                : QrErrorCorrectionOption.Medium;
            var qrMarginModules = values.Length > 7 && values[7] is int marginModules ? marginModules : 4;
            return LabelImageRenderer.Render(
                kind,
                content,
                barcodeFormat,
                isBarcodeTextVisible,
                quietZoneMillimeters,
                elementWidthMillimeters,
                qrErrorCorrection,
                qrMarginModules);
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
