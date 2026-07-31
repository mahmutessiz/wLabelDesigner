using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace wLabelDesigner.Controls;

public sealed class DesignerRuler : FrameworkElement
{
    private const double PixelsPerMillimeter = 96d / 25.4d;

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(DesignerRuler),
        new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
        nameof(Length),
        typeof(double),
        typeof(DesignerRuler),
        new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double Length
    {
        get => (double)GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(248, 249, 251)), null, new Rect(RenderSize));

        var pen = new Pen(new SolidColorBrush(Color.FromRgb(102, 112, 133)), 0.7);
        pen.Freeze();
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var maximum = Math.Max(0, Length);
        for (var millimeter = 0d; millimeter <= maximum; millimeter += 1)
        {
            var position = millimeter * PixelsPerMillimeter;
            var isMajor = Math.Abs(millimeter % 10) < 0.001;
            var isMedium = Math.Abs(millimeter % 5) < 0.001;
            var tickLength = isMajor ? 10d : isMedium ? 7d : 4d;

            if (Orientation == Orientation.Horizontal)
            {
                drawingContext.DrawLine(pen, new Point(position, ActualHeight), new Point(position, ActualHeight - tickLength));
            }
            else
            {
                drawingContext.DrawLine(pen, new Point(ActualWidth, position), new Point(ActualWidth - tickLength, position));
            }

            if (!isMajor)
            {
                continue;
            }

            var text = new FormattedText(
                millimeter.ToString("0", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                8,
                new SolidColorBrush(Color.FromRgb(71, 84, 103)),
                pixelsPerDip);
            if (Orientation == Orientation.Horizontal)
            {
                drawingContext.DrawText(text, new Point(position + 2, 1));
            }
            else
            {
                drawingContext.PushTransform(new RotateTransform(-90, 0, 0));
                drawingContext.DrawText(text, new Point(-position + 2, 1));
                drawingContext.Pop();
            }
        }
    }
}
