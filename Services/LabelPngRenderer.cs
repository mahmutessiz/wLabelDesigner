using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public static class LabelPngRenderer
{
    private const long MaximumPixelCount = 40_000_000;

    public static byte[] Render(LabelDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var dpi = document.PrinterDpi is 203 or 300 ? document.PrinterDpi : 203;
        var widthMillimeters = NormalizeDimension(document.WidthMillimeters);
        var heightMillimeters = NormalizeDimension(document.HeightMillimeters);
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(widthMillimeters / 25.4d * dpi));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(heightMillimeters / 25.4d * dpi));
        if ((long)pixelWidth * pixelHeight > MaximumPixelCount)
        {
            throw new InvalidOperationException("The label is too large to export as a PNG at the selected DPI.");
        }

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawDrawing(LabelDrawingRenderer.CreateDrawing(document));
        }

        var bitmap = new RenderTargetBitmap(
            pixelWidth,
            pixelHeight,
            dpi,
            dpi,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static double NormalizeDimension(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 1, 1000) : 1;
}
