using wLabelDesigner.Services;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class DesignerSnapEngineTests
{
    [Fact]
    public void SnapMovement_SnapsSelectionEdgeToLabelEdge()
    {
        var result = DesignerSnapEngine.SnapMovement(
            new DesignerBounds(10, 5, 20, 10),
            horizontalChange: -9.2,
            verticalChange: 0,
            labelWidth: 100,
            labelHeight: 50,
            gridSize: 5,
            threshold: 1,
            otherElements: []);

        Assert.Equal(-10, result.HorizontalChange, precision: 6);
        Assert.Equal(0, result.VerticalGuide);
    }

    [Fact]
    public void SnapMovement_SnapsCentersToAnotherElementAndReturnsGuides()
    {
        var result = DesignerSnapEngine.SnapMovement(
            new DesignerBounds(10, 5, 10, 6),
            horizontalChange: 24.4,
            verticalChange: 16.6,
            labelWidth: 100,
            labelHeight: 50,
            gridSize: 5,
            threshold: 1,
            otherElements: [new DesignerBounds(30, 20, 20, 10)]);

        Assert.Equal(25, result.HorizontalChange, precision: 6);
        Assert.Equal(17, result.VerticalChange, precision: 6);
        Assert.Equal(40, result.VerticalGuide);
        Assert.Equal(25, result.HorizontalGuide);
    }

    [Fact]
    public void SnapMovement_UsesGridWhenNoAlignmentTargetIsClose()
    {
        var result = DesignerSnapEngine.SnapMovement(
            new DesignerBounds(12, 12, 10, 10),
            horizontalChange: 0.6,
            verticalChange: 2.4,
            labelWidth: 120,
            labelHeight: 80,
            gridSize: 5,
            threshold: 2.5,
            otherElements: []);

        Assert.Equal(3, result.HorizontalChange, precision: 6);
        Assert.Equal(3, result.VerticalChange, precision: 6);
        Assert.Null(result.VerticalGuide);
        Assert.Null(result.HorizontalGuide);
    }

    [Fact]
    public void SnapMovement_DoesNotSnapOutsideThreshold()
    {
        var result = DesignerSnapEngine.SnapMovement(
            new DesignerBounds(12, 12, 10, 10),
            horizontalChange: 0.2,
            verticalChange: 0.2,
            labelWidth: 100,
            labelHeight: 50,
            gridSize: 5,
            threshold: 1,
            otherElements: []);

        Assert.Equal(0.2, result.HorizontalChange, precision: 6);
        Assert.Equal(0.2, result.VerticalChange, precision: 6);
    }
}
