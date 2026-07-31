namespace wLabelDesigner.Models;

public sealed class LabelPrintSettings
{
    public int Copies { get; set; } = 1;

    public string? PrinterName { get; set; }
}
