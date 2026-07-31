namespace wLabelDesigner.Services;

public readonly record struct DesignerBounds(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + (Width / 2);
    public double CenterY => Y + (Height / 2);
}

public readonly record struct SnapResult(
    double HorizontalChange,
    double VerticalChange,
    double? VerticalGuide,
    double? HorizontalGuide);

public static class DesignerSnapEngine
{
    public static SnapResult SnapMovement(
        DesignerBounds selection,
        double horizontalChange,
        double verticalChange,
        double labelWidth,
        double labelHeight,
        double gridSize,
        double threshold,
        IReadOnlyList<DesignerBounds> otherElements)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(labelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(labelHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gridSize);
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        ArgumentNullException.ThrowIfNull(otherElements);

        var moved = selection with
        {
            X = selection.X + horizontalChange,
            Y = selection.Y + verticalChange
        };

        var verticalTargets = new List<double> { 0, labelWidth / 2, labelWidth };
        var horizontalTargets = new List<double> { 0, labelHeight / 2, labelHeight };
        foreach (var element in otherElements)
        {
            verticalTargets.AddRange([element.X, element.CenterX, element.Right]);
            horizontalTargets.AddRange([element.Y, element.CenterY, element.Bottom]);
        }

        var horizontalSnap = FindBestSnap(
            [moved.X, moved.CenterX, moved.Right],
            verticalTargets,
            threshold);
        var verticalSnap = FindBestSnap(
            [moved.Y, moved.CenterY, moved.Bottom],
            horizontalTargets,
            threshold);

        var gridHorizontalAdjustment = RoundToGrid(moved.X, gridSize) - moved.X;
        var gridVerticalAdjustment = RoundToGrid(moved.Y, gridSize) - moved.Y;

        var xAdjustment = horizontalSnap is not null
            ? horizontalSnap.Value.Adjustment
            : Math.Abs(gridHorizontalAdjustment) <= threshold ? gridHorizontalAdjustment : 0;
        var yAdjustment = verticalSnap is not null
            ? verticalSnap.Value.Adjustment
            : Math.Abs(gridVerticalAdjustment) <= threshold ? gridVerticalAdjustment : 0;

        return new SnapResult(
            horizontalChange + xAdjustment,
            verticalChange + yAdjustment,
            horizontalSnap?.Target,
            verticalSnap?.Target);
    }

    private static (double Adjustment, double Target)? FindBestSnap(
        IReadOnlyList<double> movingPoints,
        IReadOnlyList<double> targets,
        double threshold)
    {
        (double Adjustment, double Target)? best = null;
        foreach (var movingPoint in movingPoints)
        {
            foreach (var target in targets)
            {
                var adjustment = target - movingPoint;
                if (Math.Abs(adjustment) <= threshold &&
                    (best is null || Math.Abs(adjustment) < Math.Abs(best.Value.Adjustment)))
                {
                    best = (adjustment, target);
                }
            }
        }

        return best;
    }

    private static double RoundToGrid(double value, double gridSize) =>
        Math.Round(value / gridSize, MidpointRounding.AwayFromZero) * gridSize;
}
