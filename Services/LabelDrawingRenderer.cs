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
