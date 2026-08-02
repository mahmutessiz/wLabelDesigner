using System.Globalization;
using System.Windows;
using System.Windows.Media;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public readonly record struct PrintLayout(
    double PrintableX,
    double PrintableY,
    double PrintableWidth,
    double PrintableHeight,
    double ContentOffsetX,
    double ContentOffsetY);

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

    public static DrawingImage CreatePrintImage(LabelDocument document, LabelPrintSettings settings)
    {
        var drawing = CreatePrintDrawing(document, settings);
        drawing.Freeze();
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    public static DrawingGroup CreatePrintDrawing(LabelDocument document, LabelPrintSettings settings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(settings);

        var pageWidth = document.WidthMillimeters * DeviceIndependentPixelsPerMillimeter;
        var pageHeight = document.HeightMillimeters * DeviceIndependentPixelsPerMillimeter;
        var layout = CalculatePrintLayout(document, settings);
        var printableBounds = new Rect(
            layout.PrintableX * DeviceIndependentPixelsPerMillimeter,
            layout.PrintableY * DeviceIndependentPixelsPerMillimeter,
            layout.PrintableWidth * DeviceIndependentPixelsPerMillimeter,
            layout.PrintableHeight * DeviceIndependentPixelsPerMillimeter);

        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        context.DrawRectangle(Brushes.White, null, new Rect(0, 0, pageWidth, pageHeight));
        if (printableBounds.IsEmpty || printableBounds.Width <= 0 || printableBounds.Height <= 0)
        {
            return drawing;
        }

        context.PushClip(new RectangleGeometry(printableBounds));
        context.PushTransform(new TranslateTransform(
            layout.ContentOffsetX * DeviceIndependentPixelsPerMillimeter,
            layout.ContentOffsetY * DeviceIndependentPixelsPerMillimeter));
        context.DrawDrawing(CreateDrawing(document));
        context.Pop();
        context.Pop();
        return drawing;
    }

    public static PrintLayout CalculatePrintLayout(LabelDocument document, LabelPrintSettings settings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(settings);

        var left = NormalizeMargin(settings.MarginLeftMillimeters, document.WidthMillimeters);
        var top = NormalizeMargin(settings.MarginTopMillimeters, document.HeightMillimeters);
        var right = NormalizeMargin(settings.MarginRightMillimeters, Math.Max(0, document.WidthMillimeters - left));
        var bottom = NormalizeMargin(settings.MarginBottomMillimeters, Math.Max(0, document.HeightMillimeters - top));
        return new PrintLayout(
            left,
            top,
            Math.Max(0, document.WidthMillimeters - left - right),
            Math.Max(0, document.HeightMillimeters - top - bottom),
            left + NormalizeOffset(settings.OffsetXMillimeters),
            top + NormalizeOffset(settings.OffsetYMillimeters));
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

        if (element.Kind is LabelElementKind.Ellipse or LabelElementKind.Triangle or LabelElementKind.Diamond)
        {
            var pen = new Pen(Brushes.Black, element.StrokeThickness);
            var inset = pen.Thickness / 2;
            var strokeBounds = new Rect(
                bounds.Left + inset,
                bounds.Top + inset,
                Math.Max(0, bounds.Width - pen.Thickness),
                Math.Max(0, bounds.Height - pen.Thickness));

            if (element.Kind == LabelElementKind.Ellipse)
            {
                drawingContext.DrawEllipse(
                    null,
                    pen,
                    new Point(strokeBounds.Left + (strokeBounds.Width / 2), strokeBounds.Top + (strokeBounds.Height / 2)),
                    strokeBounds.Width / 2,
                    strokeBounds.Height / 2);
                return;
            }

            var points = element.Kind == LabelElementKind.Triangle
                ? new[] { new Point(strokeBounds.Left + (strokeBounds.Width / 2), strokeBounds.Top), strokeBounds.BottomRight, strokeBounds.BottomLeft }
                : new[]
                {
                    new Point(strokeBounds.Left + (strokeBounds.Width / 2), strokeBounds.Top),
                    new Point(strokeBounds.Right, strokeBounds.Top + (strokeBounds.Height / 2)),
                    new Point(strokeBounds.Left + (strokeBounds.Width / 2), strokeBounds.Bottom),
                    new Point(strokeBounds.Left, strokeBounds.Top + (strokeBounds.Height / 2))
                };
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(points[0], isFilled: false, isClosed: true);
                geometryContext.PolyLineTo(points[1..], isStroked: true, isSmoothJoin: true);
            }
            geometry.Freeze();
            drawingContext.DrawGeometry(null, pen, geometry);
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

    private static double NormalizeMargin(double value, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, 0, Math.Max(0, maximum)) : 0;

    private static double NormalizeOffset(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, -1000, 1000) : 0;
}
