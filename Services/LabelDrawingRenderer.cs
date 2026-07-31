using System.Globalization;
using System.Windows;
using System.Windows.Media;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public static class LabelDrawingRenderer
{
    public const double DeviceIndependentPixelsPerMillimeter = 96d / 25.4d;

    public static DrawingImage CreateImage(LabelDocument document)
    {
        var drawing = CreateDrawing(document);
        drawing.Freeze();
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    public static DrawingGroup CreateDrawing(LabelDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var drawing = new DrawingGroup();
        using var drawingContext = drawing.Open();
        var labelBounds = new Rect(
            0,
            0,
            document.WidthMillimeters * DeviceIndependentPixelsPerMillimeter,
            document.HeightMillimeters * DeviceIndependentPixelsPerMillimeter);

        drawingContext.DrawRectangle(Brushes.White, null, labelBounds);
        drawingContext.PushClip(new RectangleGeometry(labelBounds));
        foreach (var element in document.Elements)
        {
            if (!element.IsVisible)
            {
                continue;
            }

            DrawElement(drawingContext, element);
        }

        drawingContext.Pop();
        return drawing;
    }

    private static void DrawElement(DrawingContext drawingContext, LabelElementData element)
    {
        var bounds = new Rect(
            element.X * DeviceIndependentPixelsPerMillimeter,
            element.Y * DeviceIndependentPixelsPerMillimeter,
            element.Width * DeviceIndependentPixelsPerMillimeter,
            element.Height * DeviceIndependentPixelsPerMillimeter);

        var rotation = double.IsFinite(element.RotationDegrees) ? element.RotationDegrees : 0;
        var isRotated = Math.Abs(rotation) > 0.001;
        if (isRotated)
        {
            drawingContext.PushTransform(new RotateTransform(rotation, bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2)));
        }

        try
        {
            DrawUnrotatedElement(drawingContext, element, bounds);
        }
        finally
        {
            if (isRotated)
            {
                drawingContext.Pop();
            }
        }
    }

    private static void DrawUnrotatedElement(
        DrawingContext drawingContext,
        LabelElementData element,
        Rect bounds)
    {

        if (element.Kind is LabelElementKind.Rectangle or LabelElementKind.RoundedRectangle)
        {
            var pen = new Pen(Brushes.Black, element.StrokeThickness);
            var inset = pen.Thickness / 2;
            var strokeBounds = new Rect(
                bounds.Left + inset,
                bounds.Top + inset,
                Math.Max(0, bounds.Width - pen.Thickness),
                Math.Max(0, bounds.Height - pen.Thickness));
            var radius = element.Kind == LabelElementKind.RoundedRectangle
                ? 3 * DeviceIndependentPixelsPerMillimeter
                : 0;
            drawingContext.DrawRoundedRectangle(null, pen, strokeBounds, radius, radius);
            return;
        }

        if (element.Kind == LabelElementKind.Line)
        {
            var start = element.IsLineDirectionReversed ? bounds.BottomLeft : bounds.TopLeft;
            var end = element.IsLineDirectionReversed ? bounds.TopRight : bounds.BottomRight;
            drawingContext.DrawLine(
                new Pen(Brushes.Black, element.StrokeThickness),
                start,
                end);
            return;
        }

        if (element.Kind == LabelElementKind.Text)
        {
            drawingContext.PushClip(new RectangleGeometry(bounds));
            var text = new FormattedText(
                element.Content,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily(element.FontFamily),
                    element.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                    element.IsBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStretches.Normal),
                element.FontSize,
                Brushes.Black,
                pixelsPerDip: 1)
            {
                MaxTextWidth = bounds.Width,
                MaxTextHeight = bounds.Height,
                TextAlignment = element.TextAlignment switch
                {
                    TextAlignmentOption.Left => TextAlignment.Left,
                    TextAlignmentOption.Right => TextAlignment.Right,
                    _ => TextAlignment.Center
                }
            };

            if (element.IsUnderlined)
            {
                text.SetTextDecorations(TextDecorations.Underline);
            }

            drawingContext.DrawText(text, bounds.TopLeft);
            drawingContext.Pop();
            return;
        }

        var image = LabelImageRenderer.Render(element.Kind, element.Content);
        if (image is not null)
        {
            drawingContext.DrawImage(image, bounds);
        }
    }
}
