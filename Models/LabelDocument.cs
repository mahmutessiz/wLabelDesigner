namespace wLabelDesigner.Models;

public sealed class LabelDocument
{
    public const int CurrentFormatVersion = 3;

    public int FormatVersion { get; init; } = CurrentFormatVersion;

    public string Name { get; set; } = "Untitled label";

    public double WidthMillimeters { get; set; } = 100;

    public double HeightMillimeters { get; set; } = 50;

    public int PrinterDpi { get; set; } = 203;

    public LabelPrintSettings PrintSettings { get; set; } = new();

    public List<LabelElementData> Elements { get; init; } = [];
}
