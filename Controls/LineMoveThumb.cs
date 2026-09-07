using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using wLabelDesigner.Models;

namespace wLabelDesigner.Controls;

// Draw directly rather than using Shape: a Shape's layout clip can erase a
// horizontal/vertical line or cut off a stroke wider than its endpoint bounds.
public sealed class LineMoveThumb : Thumb
{
    public static readonly DependencyProperty IsReversedProperty = DependencyProperty.Register(
        nameof(IsReversed), typeof(bool), typeof(LineMoveThumb), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ZoomScaleProperty = DependencyProperty.Register(
        nameof(ZoomScale), typeof(double), typeof(LineMoveThumb), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(LineMoveThumb), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(LineMoveThumb), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeStyleProperty = DependencyProperty.Register(
        nameof(StrokeStyle), typeof(StrokeStyleOption), typeof(LineMoveThumb), new FrameworkPropertyMetadata(StrokeStyleOption.Solid, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty DrawingInsetProperty = DependencyProperty.Register(
        nameof(DrawingInset), typeof(double), typeof(LineMoveThumb), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsReversed { get => (bool)GetValue(IsReversedProperty); set => SetValue(IsReversedProperty, value); }
    public double ZoomScale { get => (double)GetValue(ZoomScaleProperty); set => SetValue(ZoomScaleProperty, value); }
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    public StrokeStyleOption StrokeStyle { get => (StrokeStyleOption)GetValue(StrokeStyleProperty); set => SetValue(StrokeStyleProperty, value); }
    public double DrawingInset { get => (double)GetValue(DrawingInsetProperty); set => SetValue(DrawingInsetProperty, value); }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var thickness = double.IsFinite(StrokeThickness) ? Math.Clamp(StrokeThickness, 0.25, 20) : 1;
        var start = new Point(DrawingInset, IsReversed ? ActualHeight - DrawingInset : DrawingInset);
        var end = new Point(ActualWidth - DrawingInset, IsReversed ? DrawingInset : ActualHeight - DrawingInset);
        var pen = new Pen(Brushes.Transparent, Math.Max(thickness, 16 / Math.Max(0.1, ZoomScale)))
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        drawingContext.DrawLine(pen, start, end);
        drawingContext.DrawLine(new Pen(Stroke, thickness)
        {
            DashStyle = StrokeStyle switch
            {
                StrokeStyleOption.Dashed => DashStyles.Dash,
                StrokeStyleOption.Dotted => DashStyles.Dot,
                _ => DashStyles.Solid
            },
            DashCap = PenLineCap.Round
        }, start, end);
    }
}
