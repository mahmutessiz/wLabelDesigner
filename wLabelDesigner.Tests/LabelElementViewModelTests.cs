using wLabelDesigner.Models;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class LabelElementViewModelTests
{
    [Theory]
    [InlineData(20, 0, false, 0, 0)]
    [InlineData(0, 20, false, 0, 90)]
    [InlineData(20, 20, true, 0, -45)]
    [InlineData(20, 20, false, 15, 60)]
    [InlineData(20, 20, false, 180, -135)]
    public void DisplayRotation_CombinesEndpointDirectionAndBoxRotation(double width, double height, bool reversed, double rotation, double expected)
    {
        var line = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.Line,
            Width = width,
            Height = height,
            IsLineDirectionReversed = reversed,
            RotationDegrees = rotation
        });
        Assert.Equal(expected, line.DisplayRotationDegrees);
        line.DisplayRotationDegrees = 30;
        Assert.Equal(30, line.DisplayRotationDegrees, 2);
        Assert.Equal(width, line.Width);
        Assert.Equal(height, line.Height);
        Assert.Equal(30, new LabelElementViewModel(line.ToData()).DisplayRotationDegrees, 2);
    }

    [Fact]
    public void EndpointEdits_NotifyDisplayedRotation()
    {
        var line = new LabelElementViewModel(new LabelElementData { Kind = LabelElementKind.Line, Width = 20, Height = 0 });
        var count = 0;
        line.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(line.DisplayRotationDegrees)) count++; };
        line.Height = 20;
        Assert.Equal(45, line.DisplayRotationDegrees);
        line.IsLineDirectionReversed = true;
        Assert.Equal(-45, line.DisplayRotationDegrees);
        line.Width = 0;
        Assert.Equal(-90, line.DisplayRotationDegrees);
        Assert.Equal(3, count);
    }

    [Theory]
    [InlineData(LabelElementKind.Line)]
    [InlineData(LabelElementKind.Text)]
    public void RotationChange_NotifiesBindingsAndPreservesGeometry(LabelElementKind kind)
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = kind,
            X = 10,
            Y = 12,
            Width = 20,
            Height = 1
        });
        var notifications = new List<string?>();
        element.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        element.RotationDegrees = 90;

        Assert.Contains(nameof(element.RotationDegrees), notifications);
        Assert.Equal(90, element.ToData().RotationDegrees);
        Assert.Equal(10, element.X);
        Assert.Equal(12, element.Y);
        Assert.Equal(20, element.Width);
        Assert.Equal(1, element.Height);
    }

    [Theory]
    [InlineData(270, -90)]
    [InlineData(-270, 90)]
    [InlineData(540, 180)]
    [InlineData(-180, -180)]
    public void RotationDegrees_NormalizesToDocumentRange(double input, double expected)
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.Text,
            Content = "Rotated"
        });

        element.RotationDegrees = input;

        Assert.Equal(expected, element.RotationDegrees);
        Assert.Equal(expected, element.ToData().RotationDegrees);
    }

    [Fact]
    public void Constructor_ReplacesInvalidRotationWithZero()
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            RotationDegrees = double.NaN
        });

        Assert.Equal(0, element.RotationDegrees);
    }
}
