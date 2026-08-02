namespace wLabelDesigner.Models;

public sealed class LabelPrintSettings
{
    public int Copies { get; set; } = 1;

    public string? PrinterName { get; set; }

    public double MarginLeftMillimeters { get; set; }

    public double MarginTopMillimeters { get; set; }

    public double MarginRightMillimeters { get; set; }

    public double MarginBottomMillimeters { get; set; }

    public double OffsetXMillimeters { get; set; }

    public double OffsetYMillimeters { get; set; }
}
