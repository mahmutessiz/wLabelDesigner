using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Printing;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using wLabelDesigner.Models;
using wLabelDesigner.Services;

namespace wLabelDesigner.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ILabelDocumentStore documentStore;
    private readonly IFileDialogService fileDialogService;
    private readonly ILabelPrintService printService;
    private string? currentPath;
    private bool isLoading;

    public MainViewModel(
        ILabelDocumentStore documentStore,
        IFileDialogService fileDialogService,
        ILabelPrintService printService)
    {
        this.documentStore = documentStore;
        this.fileDialogService = fileDialogService;
        this.printService = printService;
        NewDocument();
    }

    public ObservableCollection<LabelElementViewModel> Elements { get; } = [];

    public IReadOnlyList<int> SupportedDpi { get; } = [203, 300];

    public IReadOnlyList<string> AvailableFontFamilies { get; } =
        ["Arial", "Calibri", "Consolas", "Segoe UI", "Tahoma", "Times New Roman", "Verdana"];

    public IReadOnlyList<double> CommonFontSizes { get; } = [8, 9, 10, 11, 12, 14, 16, 18, 24, 32, 48, 64, 72];

    public bool HasTextSelection => SelectedElement?.Kind == LabelElementKind.Text;

    public bool HasDataElementSelection => SelectedElement?.Kind is LabelElementKind.Barcode or LabelElementKind.QrCode;

    public string WindowTitle => $"{DocumentName}{(IsDirty ? " *" : string.Empty)} — FckBarTender";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string documentName = "Untitled label";

    private double labelWidth = 100;

    public double LabelWidth
    {
        get => labelWidth;
        set
        {
            if (SetProperty(ref labelWidth, NormalizeDimension(value)))
            {
                MarkDirty();
            }
        }
    }

    private double labelHeight = 50;

    public double LabelHeight
    {
        get => labelHeight;
        set
        {
            if (SetProperty(ref labelHeight, NormalizeDimension(value)))
            {
                MarkDirty();
            }
        }
    }

    [ObservableProperty]
    private int printerDpi = 203;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyPropertyChangedFor(nameof(HasTextSelection))]
    [NotifyPropertyChangedFor(nameof(HasDataElementSelection))]
    private LabelElementViewModel? selectedElement;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool isDirty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    partial void OnDocumentNameChanged(string value) => MarkDirty();

    partial void OnPrinterDpiChanged(int value) => MarkDirty();

    [RelayCommand]
    private void NewDocument()
    {
        UnsubscribeFromElements();
        Elements.Clear();
        isLoading = true;
        DocumentName = "Untitled label";
        LabelWidth = 100;
        LabelHeight = 50;
        PrinterDpi = 203;
        isLoading = false;
        currentPath = null;
        SelectedElement = null;
        IsDirty = false;
        StatusMessage = "New 100 × 50 mm label";
    }

    [RelayCommand]
    private void AddText() => AddElementAt(LabelElementKind.Text, 5, 5, beginEditing: true);

    [RelayCommand]
    private void AddBarcode() => AddElementAt(LabelElementKind.Barcode, 5, 5);

    [RelayCommand]
    private void AddQrCode() => AddElementAt(LabelElementKind.QrCode, 5, 5);

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private void DeleteSelected()
    {
        if (SelectedElement is null)
        {
            return;
        }

        SelectedElement.PropertyChanged -= OnElementPropertyChanged;
        Elements.Remove(SelectedElement);
        SelectedElement = null;
        MarkDirty();
        StatusMessage = "Element deleted";
    }

    private bool CanDeleteSelected() => SelectedElement is not null;

    [RelayCommand]
    private async Task OpenAsync()
    {
        var path = fileDialogService.ChooseTemplateToOpen();
        if (path is null)
        {
            return;
        }

        try
        {
            var document = await documentStore.LoadAsync(path);
            LoadDocument(document);
            currentPath = path;
            IsDirty = false;
            StatusMessage = $"Opened {Path.GetFileName(path)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusMessage = $"Could not open template: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var suggestedName = MakeSafeFileName(DocumentName) + ".fckbartndr";
        var path = currentPath ?? fileDialogService.ChooseTemplateToSave(suggestedName);
        if (path is null)
        {
            return;
        }

        try
        {
            await documentStore.SaveAsync(path, CreateDocument());
            currentPath = path;
            IsDirty = false;
            StatusMessage = $"Saved {Path.GetFileName(path)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not save template: {exception.Message}";
        }
    }

    [RelayCommand]
    private void Print()
    {
        try
        {
            StatusMessage = printService.Print(CreateDocument())
                ? "Label sent to printer"
                : "Printing cancelled";
        }
        catch (Exception exception) when (exception is PrintSystemException or InvalidOperationException)
        {
            StatusMessage = $"Could not print label: {exception.Message}";
        }
    }

    public LabelElementViewModel AddElementAt(
        LabelElementKind kind,
        double x,
        double y,
        bool beginEditing = false)
    {
        var (content, width, height) = kind switch
        {
            LabelElementKind.Text => ("Text", 40d, 10d),
            LabelElementKind.Barcode => ("123456789012", 50d, 16d),
            LabelElementKind.QrCode => ("https://example.com", 22d, 22d),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported label element type.")
        };

        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = kind,
            Content = content,
            X = Math.Clamp(x, 0, Math.Max(0, LabelWidth - width)),
            Y = Math.Clamp(y, 0, Math.Max(0, LabelHeight - height)),
            Width = width,
            Height = height
        })
        {
            IsEditing = beginEditing
        };

        element.PropertyChanged += OnElementPropertyChanged;
        Elements.Add(element);
        SelectedElement = element;
        MarkDirty();
        StatusMessage = $"{element.DisplayName} added";
        return element;
    }

    private LabelDocument CreateDocument() => new()
    {
        Name = DocumentName,
        WidthMillimeters = Math.Clamp(LabelWidth, 1, 1000),
        HeightMillimeters = Math.Clamp(LabelHeight, 1, 1000),
        PrinterDpi = SupportedDpi.Contains(PrinterDpi) ? PrinterDpi : 203,
        Elements = Elements.Select(element => element.ToData()).ToList()
    };

    private void LoadDocument(LabelDocument document)
    {
        if (document.FormatVersion > LabelDocument.CurrentFormatVersion)
        {
            throw new InvalidDataException($"Template format {document.FormatVersion} is not supported.");
        }

        UnsubscribeFromElements();
        Elements.Clear();
        isLoading = true;
        DocumentName = string.IsNullOrWhiteSpace(document.Name) ? "Untitled label" : document.Name;
        LabelWidth = Math.Clamp(document.WidthMillimeters, 1, 1000);
        LabelHeight = Math.Clamp(document.HeightMillimeters, 1, 1000);
        PrinterDpi = SupportedDpi.Contains(document.PrinterDpi) ? document.PrinterDpi : 203;

        foreach (var data in document.Elements)
        {
            var element = new LabelElementViewModel(data);
            element.PropertyChanged += OnElementPropertyChanged;
            Elements.Add(element);
        }

        SelectedElement = null;
        isLoading = false;
    }

    private void UnsubscribeFromElements()
    {
        foreach (var element in Elements)
        {
            element.PropertyChanged -= OnElementPropertyChanged;
        }
    }

    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LabelElementViewModel.IsEditing))
        {
            MarkDirty();
        }
    }

    private void MarkDirty()
    {
        if (!isLoading)
        {
            IsDirty = true;
        }
    }

    private static string MakeSafeFileName(string name)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Where(character => !invalidCharacters.Contains(character)).ToArray()).Trim();
        return string.IsNullOrEmpty(safeName) ? "Untitled label" : safeName;
    }

    private static double NormalizeDimension(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 1, 1000) : 1;
}
