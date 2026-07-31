using System.Globalization;
using System.Windows.Data;

namespace wLabelDesigner.Converters;

public sealed class MillimetersToDeviceIndependentPixelsConverter : IValueConverter
{
    private const double DeviceIndependentPixelsPerMillimeter = 96d / 25.4d;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double millimeters ? millimeters * DeviceIndependentPixelsPerMillimeter : 0d;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double pixels ? pixels / DeviceIndependentPixelsPerMillimeter : 0d;
}
