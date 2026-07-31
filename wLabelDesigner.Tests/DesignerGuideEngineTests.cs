using wLabelDesigner.Services;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class DesignerGuideEngineTests
{
    [Fact]
    public void FindGuides_ReturnsLabelEdgeWithinThreshold()
    {
        var result = DesignerGuideEngine.FindGuides(
            new DesignerBounds(10, 5, 20, 10),
            horizontalChange: -9.2,
            verticalChange: 0,
            labelWidth: 100,
            labelHeight: 50,
            threshold: 1,
            otherElements: []);

        Assert.Equal(0, result.VerticalGuide);
    }

    [Fact]
    public void FindGuides_ReturnsOtherElementCentersWithinThreshold()
    {
        var result = DesignerGuideEngine.FindGuides(
            new DesignerBounds(10, 5, 10, 6),
            horizontalChange: 24.4,
            verticalChange: 16.6,
            labelWidth: 100,
            labelHeight: 50,
            threshold: 1,
            otherElements: [new DesignerBounds(30, 20, 20, 10)]);

        Assert.Equal(40, result.VerticalGuide);
        Assert.Equal(25, result.HorizontalGuide);
    }

    [Fact]
    public void FindGuides_ReturnsNothingOutsideThreshold()
    {
        var result = DesignerGuideEngine.FindGuides(
            new DesignerBounds(12, 12, 10, 10),
            horizontalChange: 0.2,
            verticalChange: 0.2,
            labelWidth: 120,
            labelHeight: 80,
            threshold: 1,
            otherElements: []);

        Assert.Null(result.VerticalGuide);
        Assert.Null(result.HorizontalGuide);
    }
}
