using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using wLabelDesigner.Models;
using wLabelDesigner.Services;

namespace wLabelDesigner.ViewModels;

public sealed partial class PrintPreviewViewModel : ObservableObject
{
    private readonly LabelDocument document;

    public PrintPreviewViewModel(
        LabelDocument document,
        IReadOnlyList<string> printers,
        string? defaultPrinter)
    {
        this.document = document;
        DocumentName = document.Name;
        LabelDescription = $"{document.WidthMillimeters:0.##} × {document.HeightMillimeters:0.##} mm · {document.PrinterDpi} DPI";
        PreviewWidth = document.WidthMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter;
        PreviewHeight = document.HeightMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter;
        Printers = printers;
        copies = Math.Clamp(document.PrintSettings.Copies, 1, 999);
        marginLeftMillimeters = NormalizeMargin(document.PrintSettings.MarginLeftMillimeters, document.WidthMillimeters);
        marginTopMillimeters = NormalizeMargin(document.PrintSettings.MarginTopMillimeters, document.HeightMillimeters);
        marginRightMillimeters = NormalizeMargin(document.PrintSettings.MarginRightMillimeters, document.WidthMillimeters);
        marginBottomMillimeters = NormalizeMargin(document.PrintSettings.MarginBottomMillimeters, document.HeightMillimeters);
        offsetXMillimeters = NormalizeOffset(document.PrintSettings.OffsetXMillimeters);
        offsetYMillimeters = NormalizeOffset(document.PrintSettings.OffsetYMillimeters);
        previewImage = LabelDrawingRenderer.CreatePrintImage(document, CreateSettings());
        selectedPrinter = printers.FirstOrDefault(printer =>
                string.Equals(printer, document.PrintSettings.PrinterName, StringComparison.OrdinalIgnoreCase))
            ?? printers.FirstOrDefault(printer =>
                string.Equals(printer, defaultPrinter, StringComparison.OrdinalIgnoreCase))
            ?? printers.FirstOrDefault();
    }

    public string DocumentName { get; }

    public string LabelDescription { get; }

    private DrawingImage previewImage;

    public DrawingImage PreviewImage
    {
        get => previewImage;
        private set => SetProperty(ref previewImage, value);
    }

    public double PreviewWidth { get; }

    public double PreviewHeight { get; }

    public IReadOnlyList<string> Printers { get; }

    public Thickness PreviewMargin => new(
        MarginLeftMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter,
        MarginTopMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter,
        MarginRightMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter,
        MarginBottomMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter);

    public bool CanPrint => SelectedPrinter is not null;

    private int copies;

    public int Copies
    {
        get => copies;
        set => SetProperty(ref copies, Math.Clamp(value, 1, 999));
    }

    private double marginLeftMillimeters;

    public double MarginLeftMillimeters
    {
        get => marginLeftMillimeters;
        set => SetCalibrationValue(ref marginLeftMillimeters, NormalizeMargin(value, document.WidthMillimeters));
    }

    private double marginTopMillimeters;

    public double MarginTopMillimeters
    {
        get => marginTopMillimeters;
        set => SetCalibrationValue(ref marginTopMillimeters, NormalizeMargin(value, document.HeightMillimeters));
    }

    private double marginRightMillimeters;

    public double MarginRightMillimeters
    {
        get => marginRightMillimeters;
        set => SetCalibrationValue(ref marginRightMillimeters, NormalizeMargin(value, document.WidthMillimeters));
    }

    private double marginBottomMillimeters;

    public double MarginBottomMillimeters
    {
        get => marginBottomMillimeters;
        set => SetCalibrationValue(ref marginBottomMillimeters, NormalizeMargin(value, document.HeightMillimeters));
    }

    private double offsetXMillimeters;

    public double OffsetXMillimeters
    {
        get => offsetXMillimeters;
        set => SetCalibrationValue(ref offsetXMillimeters, NormalizeOffset(value));
    }

    private double offsetYMillimeters;

    public double OffsetYMillimeters
    {
        get => offsetYMillimeters;
        set => SetCalibrationValue(ref offsetYMillimeters, NormalizeOffset(value));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPrint))]
    private string? selectedPrinter;

    [RelayCommand]
    private void IncreaseCopies() => Copies++;

    [RelayCommand]
    private void DecreaseCopies() => Copies--;

    private void SetCalibrationValue(
        ref double field,
        double value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        OnPropertyChanged(nameof(PreviewMargin));
        PreviewImage = LabelDrawingRenderer.CreatePrintImage(document, CreateSettings());
    }

    private LabelPrintSettings CreateSettings() => new()
    {
        MarginLeftMillimeters = MarginLeftMillimeters,
        MarginTopMillimeters = MarginTopMillimeters,
        MarginRightMillimeters = MarginRightMillimeters,
        MarginBottomMillimeters = MarginBottomMillimeters,
        OffsetXMillimeters = OffsetXMillimeters,
        OffsetYMillimeters = OffsetYMillimeters
    };

    private static double NormalizeMargin(double value, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, 0, maximum) : 0;

    private static double NormalizeOffset(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, -1000, 1000) : 0;
}
