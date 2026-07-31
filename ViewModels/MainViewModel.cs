using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    private string? currentPath;
    private bool isLoading;

    public MainViewModel(ILabelDocumentStore documentStore, IFileDialogService fileDialogService)
    {
        this.documentStore = documentStore;
        this.fileDialogService = fileDialogService;
        NewDocument();
    }

    public ObservableCollection<LabelElementViewModel> Elements { get; } = [];

    public IReadOnlyList<int> SupportedDpi { get; } = [203, 300];

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
    private void AddText() => AddElement(LabelElementKind.Text, "Sample text", 40, 10);

    [RelayCommand]
    private void AddBarcode() => AddElement(LabelElementKind.Barcode, "123456789012", 50, 16);

    [RelayCommand]
    private void AddQrCode() => AddElement(LabelElementKind.QrCode, "https://example.com", 22, 22);

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

    private void AddElement(LabelElementKind kind, string content, double width, double height)
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = kind,
            Content = content,
            X = 5,
            Y = 5,
            Width = width,
            Height = height
        });

        element.PropertyChanged += OnElementPropertyChanged;
        Elements.Add(element);
        SelectedElement = element;
        MarkDirty();
        StatusMessage = $"{element.DisplayName} added";
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

    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e) => MarkDirty();

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
