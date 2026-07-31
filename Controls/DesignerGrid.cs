using System.Windows;
using System.Windows.Media;

namespace wLabelDesigner.Controls;

public sealed class DesignerGrid : FrameworkElement
{
    private const double PixelsPerMillimeter = 96d / 25.4d;

    public static readonly DependencyProperty GridSizeProperty = DependencyProperty.Register(
        nameof(GridSize),
        typeof(double),
        typeof(DesignerGrid),
        new FrameworkPropertyMetadata(5d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double GridSize
    {
        get => (double)GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (!double.IsFinite(GridSize) || GridSize <= 0)
        {
            return;
        }

        var spacing = GridSize * PixelsPerMillimeter;
        var minorPen = new Pen(new SolidColorBrush(Color.FromArgb(42, 79, 70, 229)), 0.5);
        var majorPen = new Pen(new SolidColorBrush(Color.FromArgb(76, 79, 70, 229)), 0.75);
        minorPen.Freeze();
        majorPen.Freeze();

        var index = 0;
        for (var x = 0d; x <= ActualWidth; x += spacing, index++)
        {
            drawingContext.DrawLine(index % 5 == 0 ? majorPen : minorPen, new Point(x, 0), new Point(x, ActualHeight));
        }

        index = 0;
        for (var y = 0d; y <= ActualHeight; y += spacing, index++)
        {
            drawingContext.DrawLine(index % 5 == 0 ? majorPen : minorPen, new Point(0, y), new Point(ActualWidth, y));
        }
    }
}
