using System.Windows;
using System.Windows.Media;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class StylingTests
{
    [Fact]
    public void ViewModel_NormalizesAndRoundTripsStylingValues()
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.RoundedRectangle,
            FillColor = "#12ab34",
            StrokeColor = "#80445566",
            TextColor = "invalid",
            StrokeStyle = StrokeStyleOption.Dotted,
            Opacity = 0.45,
            CornerRadius = 7,
            VerticalTextAlignment = VerticalTextAlignmentOption.Bottom,
            LineSpacing = 1.5,
            LetterSpacing = 2.5,
            IsTextAutoFitEnabled = true
        });

        var data = element.ToData();

        Assert.Equal("#FF12AB34", data.FillColor);
        Assert.Equal("#80445566", data.StrokeColor);
        Assert.Equal("#FF000000", data.TextColor);
        Assert.Equal(StrokeStyleOption.Dotted, data.StrokeStyle);
        Assert.Equal(0.45, data.Opacity);
        Assert.Equal(7, data.CornerRadius);
        Assert.Equal(VerticalTextAlignmentOption.Bottom, data.VerticalTextAlignment);
        Assert.Equal(1.5, data.LineSpacing);
        Assert.Equal(2.5, data.LetterSpacing);
        Assert.True(data.IsTextAutoFitEnabled);
    }

    [Fact]
    public void ViewModel_ReplacesInvalidNumericStylingValuesWithSafeDefaults()
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Opacity = double.NaN,
            LineSpacing = double.PositiveInfinity,
            LetterSpacing = double.NaN
        });

        Assert.Equal(1, element.Opacity);
        Assert.Equal(1, element.LineSpacing);
        Assert.Equal(0, element.LetterSpacing);
    }

    [Fact]
    public void CreateBrush_ParsesConfiguredArgbColor()
    {
        var brush = Assert.IsType<SolidColorBrush>(
            LabelDrawingRenderer.CreateBrush("#80402010", Brushes.Black));

        Assert.Equal(Color.FromArgb(0x80, 0x40, 0x20, 0x10), brush.Color);
        Assert.True(brush.IsFrozen);
    }

    [Fact]
    public void Renderer_AppliesFillStrokeDashRadiusAndOpacity()
    {
        var document = new LabelDocument
        {
            WidthMillimeters = 50,
            HeightMillimeters = 30,
            Elements =
            [
                new LabelElementData
                {
                    Kind = LabelElementKind.RoundedRectangle,
                    X = 5,
                    Y = 5,
                    Width = 20,
                    Height = 12,
                    FillColor = "#FFFF0000",
                    StrokeColor = "#FF0000FF",
                    StrokeThickness = 2,
                    StrokeStyle = StrokeStyleOption.Dashed,
                    CornerRadius = 4,
                    Opacity = 0.4
                }
            ]
        };

        var drawing = LabelDrawingRenderer.CreateDrawing(document);
        var descendants = EnumerateDrawings(drawing).ToArray();
        var shape = descendants
            .OfType<GeometryDrawing>()
            .Single(item => item.Brush is SolidColorBrush brush && brush.Color == Colors.Red);

        Assert.Equal(Colors.Blue, Assert.IsType<SolidColorBrush>(shape.Pen!.Brush).Color);
        Assert.NotEmpty(shape.Pen.DashStyle.Dashes);
        Assert.True(Assert.IsType<RectangleGeometry>(shape.Geometry).RadiusX > 0);
        Assert.Contains(descendants.OfType<DrawingGroup>(), group => Math.Abs(group.Opacity - 0.4) < 0.001);
    }

    [Fact]
    public void TextLayout_AutoFitShrinksTextUntilItFits()
    {
        var element = new LabelElementData
        {
            Kind = LabelElementKind.Text,
            Content = "A long line of text",
            FontSize = 32,
            IsTextAutoFitEnabled = true
        };

        var result = TextLayoutEngine.Measure(element, new Size(120, 24));

        Assert.False(result.HasOverflow);
        Assert.InRange(result.EffectiveFontSize, 1, 31.99);
    }

    [Fact]
    public void TextLayout_ReportsOverflowWhenAutoFitIsDisabled()
    {
        var element = new LabelElementData
        {
            Kind = LabelElementKind.Text,
            Content = "A long line of text",
            FontSize = 32
        };

        var result = TextLayoutEngine.Measure(element, new Size(120, 24));

        Assert.True(result.HasOverflow);
        Assert.Equal(32, result.EffectiveFontSize);
    }

    [Fact]
    public void TextLayout_LetterAndLineSpacingParticipateInOverflowMeasurement()
    {
        var compact = new LabelElementData
        {
            Kind = LabelElementKind.Text,
            Content = "ABCDE",
            FontSize = 12
        };
        var spaced = new LabelElementData
        {
            Kind = LabelElementKind.Text,
            Content = "ABCDE",
            FontSize = 12,
            LetterSpacing = 12,
            LineSpacing = 2
        };

        var available = new Size(65, 25);

        Assert.False(TextLayoutEngine.Measure(compact, available).HasOverflow);
        Assert.True(TextLayoutEngine.Measure(spaced, available).HasOverflow);
    }

    [Fact]
    public void TextLayout_VerticalAlignmentChangesRenderedPosition()
    {
        var topElement = new LabelElementData
        {
            Kind = LabelElementKind.Text,
            Content = "Aligned",
            FontSize = 12,
            VerticalTextAlignment = VerticalTextAlignmentOption.Top
        };
        var bottomElement = new LabelElementData
        {
            Kind = LabelElementKind.Text,
            Content = "Aligned",
            FontSize = 12,
            VerticalTextAlignment = VerticalTextAlignmentOption.Bottom
        };

        var topDrawing = DrawText(topElement);
        var bottomDrawing = DrawText(bottomElement);

        Assert.True(bottomDrawing.Bounds.Top > topDrawing.Bounds.Top);
    }

    [Fact]
    public async Task DocumentStore_RoundTripsStylingAndPreservesOlderDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wlabel-styling-{Guid.NewGuid():N}.json");
        var store = new JsonLabelDocumentStore();
        try
        {
            var document = new LabelDocument
            {
                Elements =
                [
                    new LabelElementData
                    {
                        Kind = LabelElementKind.Text,
                        TextColor = "#FF1570EF",
                        Opacity = 0.75,
                        LineSpacing = 1.25,
                        LetterSpacing = 2,
                        IsTextAutoFitEnabled = true
                    }
                ]
            };
            await store.SaveAsync(path, document);

            var loaded = await store.LoadAsync(path);
            var element = Assert.Single(loaded.Elements);

            Assert.Equal(LabelDocument.CurrentFormatVersion, loaded.FormatVersion);
            Assert.Equal("#FF1570EF", element.TextColor);
            Assert.Equal(0.75, element.Opacity);
            Assert.Equal(1.25, element.LineSpacing);
            Assert.Equal(2, element.LetterSpacing);
            Assert.True(element.IsTextAutoFitEnabled);

            await File.WriteAllTextAsync(path, """
                {
                  "FormatVersion": 4,
                  "Elements": [{ "Kind": "Text", "Content": "Old" }]
                }
                """);
            var older = await store.LoadAsync(path);
            var olderElement = Assert.Single(older.Elements);

            Assert.Equal("#FF000000", olderElement.TextColor);
            Assert.Equal("#00FFFFFF", olderElement.FillColor);
            Assert.Equal(1, olderElement.Opacity);
            Assert.Equal(1, olderElement.LineSpacing);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static DrawingGroup DrawText(LabelElementData element)
    {
        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        TextLayoutEngine.Draw(context, element, new Rect(0, 0, 100, 80), Brushes.Black);
        return drawing;
    }

    private static IEnumerable<Drawing> EnumerateDrawings(Drawing drawing)
    {
        yield return drawing;
        if (drawing is not DrawingGroup group)
        {
            yield break;
        }

        foreach (var child in group.Children)
        {
            foreach (var descendant in EnumerateDrawings(child))
            {
                yield return descendant;
            }
        }
    }
}
