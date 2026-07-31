using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using wLabelDesigner.Models;
using wLabelDesigner.Services;

namespace wLabelDesigner.ViewModels;

public sealed partial class PrintPreviewViewModel : ObservableObject
{
    public PrintPreviewViewModel(
        LabelDocument document,
        IReadOnlyList<string> printers,
        string? defaultPrinter)
    {
        DocumentName = document.Name;
        LabelDescription = $"{document.WidthMillimeters:0.##} × {document.HeightMillimeters:0.##} mm · {document.PrinterDpi} DPI";
        PreviewImage = LabelDrawingRenderer.CreateImage(document);
        PreviewWidth = document.WidthMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter;
        PreviewHeight = document.HeightMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter;
        Printers = printers;
        copies = Math.Clamp(document.PrintSettings.Copies, 1, 999);
        selectedPrinter = printers.FirstOrDefault(printer =>
                string.Equals(printer, document.PrintSettings.PrinterName, StringComparison.OrdinalIgnoreCase))
            ?? printers.FirstOrDefault(printer =>
                string.Equals(printer, defaultPrinter, StringComparison.OrdinalIgnoreCase))
            ?? printers.FirstOrDefault();
    }

    public string DocumentName { get; }

    public string LabelDescription { get; }

    public DrawingImage PreviewImage { get; }

    public double PreviewWidth { get; }

    public double PreviewHeight { get; }

    public IReadOnlyList<string> Printers { get; }

    public bool CanPrint => SelectedPrinter is not null;

    private int copies;

    public int Copies
    {
        get => copies;
        set => SetProperty(ref copies, Math.Clamp(value, 1, 999));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPrint))]
    private string? selectedPrinter;

    [RelayCommand]
    private void IncreaseCopies() => Copies++;

    [RelayCommand]
    private void DecreaseCopies() => Copies--;
}
