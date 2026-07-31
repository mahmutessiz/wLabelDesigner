namespace wLabelDesigner.Models;

public sealed class LabelElementData
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public LabelElementKind Kind { get; init; }

    public string Content { get; set; } = string.Empty;

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; } = 30;

    public double Height { get; set; } = 10;

    public double FontSize { get; set; } = 12;
}
