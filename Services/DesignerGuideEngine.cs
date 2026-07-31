namespace wLabelDesigner.Services;

public readonly record struct DesignerBounds(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + (Width / 2);
    public double CenterY => Y + (Height / 2);
}

public readonly record struct AlignmentGuideResult(double? VerticalGuide, double? HorizontalGuide);

public static class DesignerGuideEngine
{
    public static AlignmentGuideResult FindGuides(
        DesignerBounds selection,
        double horizontalChange,
        double verticalChange,
        double labelWidth,
        double labelHeight,
        double threshold,
        IReadOnlyList<DesignerBounds> otherElements)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(labelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(labelHeight);
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

        return new AlignmentGuideResult(
            FindClosestTarget([moved.X, moved.CenterX, moved.Right], verticalTargets, threshold),
            FindClosestTarget([moved.Y, moved.CenterY, moved.Bottom], horizontalTargets, threshold));
    }

    private static double? FindClosestTarget(
        IReadOnlyList<double> movingPoints,
        IReadOnlyList<double> targets,
        double threshold)
    {
        (double Distance, double Target)? best = null;
        foreach (var movingPoint in movingPoints)
        {
            foreach (var target in targets)
            {
                var distance = Math.Abs(target - movingPoint);
                if (distance <= threshold && (best is null || distance < best.Value.Distance))
                {
                    best = (distance, target);
                }
            }
        }

        return best?.Target;
    }
}
