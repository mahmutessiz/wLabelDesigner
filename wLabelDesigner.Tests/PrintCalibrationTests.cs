using System.Windows.Media;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class PrintCalibrationTests
{
    [Fact]
    public void CalculatePrintLayout_AppliesMarginsAndOffsetsInMillimeters()
    {
        var document = new LabelDocument
        {
            WidthMillimeters = 100,
            HeightMillimeters = 50
        };
        var settings = new LabelPrintSettings
        {
            MarginLeftMillimeters = 2,
            MarginTopMillimeters = 3,
            MarginRightMillimeters = 4,
            MarginBottomMillimeters = 5,
            OffsetXMillimeters = -1,
            OffsetYMillimeters = 2
        };

        var layout = LabelDrawingRenderer.CalculatePrintLayout(document, settings);

        Assert.Equal(2, layout.PrintableX);
        Assert.Equal(3, layout.PrintableY);
        Assert.Equal(94, layout.PrintableWidth);
        Assert.Equal(42, layout.PrintableHeight);
        Assert.Equal(1, layout.ContentOffsetX);
        Assert.Equal(5, layout.ContentOffsetY);
    }

    [Fact]
    public void PreviewCalibration_UpdatesPreviewAndNormalizesInvalidValues()
    {
        var document = new LabelDocument
        {
            WidthMillimeters = 100,
            HeightMillimeters = 50
        };
        var viewModel = new PrintPreviewViewModel(document, ["Test printer"], "Test printer");
        var originalPreview = viewModel.PreviewImage;

        viewModel.MarginLeftMillimeters = 8;
        viewModel.MarginTopMillimeters = double.NaN;
        viewModel.OffsetXMillimeters = 5000;

        Assert.Equal(8 * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter, viewModel.PreviewMargin.Left);
        Assert.Equal(0, viewModel.MarginTopMillimeters);
        Assert.Equal(1000, viewModel.OffsetXMillimeters);
        Assert.NotSame(originalPreview, viewModel.PreviewImage);
    }

    [Theory]
    [InlineData(203, 203, 102)]
    [InlineData(300, 300, 150)]
    public void RenderPrintBitmap_UsesConfiguredPrinterResolution(
        int dpi,
        int expectedWidth,
        int expectedHeight)
    {
        var document = new LabelDocument
        {
            WidthMillimeters = 25.4,
            HeightMillimeters = 12.7,
            PrinterDpi = dpi
        };

        var bitmap = LabelPngRenderer.RenderPrintBitmap(document, new LabelPrintSettings());

        Assert.Equal(expectedWidth, bitmap.PixelWidth);
        Assert.Equal(expectedHeight, bitmap.PixelHeight);
        Assert.Equal(dpi, bitmap.DpiX, precision: 6);
        Assert.True(bitmap.IsFrozen);
    }

    [Fact]
    public void RenderPrintBitmap_AppliesCalibrationBeforeFlattening()
    {
        var document = new LabelDocument
        {
            WidthMillimeters = 25.4,
            HeightMillimeters = 25.4,
            PrinterDpi = 203,
            Elements =
            {
                new LabelElementData
                {
                    Kind = LabelElementKind.Rectangle,
                    X = 0,
                    Y = 0,
                    Width = 5,
                    Height = 5,
                    StrokeThickness = 2
                }
            }
        };

        var bitmap = LabelPngRenderer.RenderPrintBitmap(document, new LabelPrintSettings
        {
            MarginLeftMillimeters = 10
        });

        var stride = bitmap.PixelWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);

        for (var row = 0; row < bitmap.PixelHeight; row++)
        {
            Assert.All(pixels.AsSpan((row * stride), 70 * 4).ToArray(), value => Assert.Equal(255, value));
        }

        Assert.Contains(pixels, value => value < 255);
        Assert.Equal(PixelFormats.Pbgra32, bitmap.Format);
    }
}
