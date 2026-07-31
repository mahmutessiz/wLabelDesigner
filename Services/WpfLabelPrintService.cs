using System.Globalization;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public sealed class WpfLabelPrintService : ILabelPrintService
{
    private const double DeviceIndependentPixelsPerMillimeter = 96d / 25.4d;

    public bool Print(LabelDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        var visual = CreatePrintVisual(document);
        dialog.PrintTicket.PageMediaSize = new PageMediaSize(
            document.WidthMillimeters * DeviceIndependentPixelsPerMillimeter,
            document.HeightMillimeters * DeviceIndependentPixelsPerMillimeter);
        dialog.PrintVisual(visual, document.Name);
        return true;
    }

    private static DrawingVisual CreatePrintVisual(LabelDocument document)
    {
        var visual = new DrawingVisual();
        using var drawingContext = visual.RenderOpen();
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
        return visual;
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
                pixelsPerDip: 1);
            text.MaxTextWidth = bounds.Width;
            text.MaxTextHeight = bounds.Height;
            text.TextAlignment = element.TextAlignment switch
            {
                TextAlignmentOption.Left => TextAlignment.Left,
                TextAlignmentOption.Right => TextAlignment.Right,
                _ => TextAlignment.Center
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
