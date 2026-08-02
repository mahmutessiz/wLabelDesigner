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
}
