namespace wLabelDesigner.Models;

public sealed class PredefinedLabelLayout(
    string name,
    string description,
    double widthMillimeters,
    double heightMillimeters,
    int printerDpi,
    Func<LabelDocument> documentFactory)
{
    public string Name { get; } = name;

    public string Description { get; } = description;

    public double WidthMillimeters { get; } = widthMillimeters;

    public double HeightMillimeters { get; } = heightMillimeters;

    public int PrinterDpi { get; } = printerDpi;

    public LabelDocument CreateDocument() => documentFactory();
}
