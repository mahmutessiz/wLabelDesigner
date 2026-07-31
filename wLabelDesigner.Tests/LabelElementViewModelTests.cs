using wLabelDesigner.Models;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class LabelElementViewModelTests
{
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
