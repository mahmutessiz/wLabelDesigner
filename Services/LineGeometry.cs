using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public readonly record struct LinePoint(double X, double Y);

public static class LineGeometry
{
    public static (LinePoint Start, LinePoint End) GetEndpoints(LabelElementData line)
    {
        var center = new LinePoint(line.X + line.Width / 2, line.Y + line.Height / 2);
        var angle = line.RotationDegrees * Math.PI / 180;
        var dx = line.Width / 2;
        var dy = (line.IsLineDirectionReversed ? -1 : 1) * line.Height / 2;
        var x = dx * Math.Cos(angle) - dy * Math.Sin(angle);
        var y = dx * Math.Sin(angle) + dy * Math.Cos(angle);
        return (new(center.X - x, center.Y - y), new(center.X + x, center.Y + y));
    }

    public static LinePoint ConstrainEndpoint(LinePoint pointer, LinePoint anchor, double width, double height, bool snap)
    {
        var x = Math.Clamp(pointer.X, 0, width);
        var y = Math.Clamp(pointer.Y, 0, height);
        if (snap)
        {
            if (Math.Abs(x - anchor.X) >= Math.Abs(y - anchor.Y)) y = anchor.Y;
            else x = anchor.X;
        }
        return new(x, y);
    }
}
