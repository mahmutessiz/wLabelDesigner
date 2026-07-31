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
    private readonly UndoHistory history = new();
    private string? currentPath;
    private bool isLoading;
    private bool isRestoringHistory;
    private string savedDocumentFingerprint = string.Empty;
    private int printCopies = 1;
    private string? printPrinterName;

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

    public IReadOnlyList<double> CommonStrokeWidths { get; } = [0.5, 0.75, 1, 1.5, 2, 3, 4, 6];

    public bool HasTextSelection => SelectedElement?.Kind == LabelElementKind.Text;

    public bool HasDataElementSelection => SelectedElement?.Kind is LabelElementKind.Barcode or LabelElementKind.QrCode;

    public bool HasShapeSelection => SelectedElement?.Kind is
        LabelElementKind.Rectangle or LabelElementKind.RoundedRectangle or LabelElementKind.Line;

    public bool HasFormattingSelection => HasTextSelection || HasShapeSelection;

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
                MarkDirty("document:width");
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
                MarkDirty("document:height");
            }
        }
    }

    [ObservableProperty]
    private int printerDpi = 203;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyPropertyChangedFor(nameof(HasTextSelection))]
    [NotifyPropertyChangedFor(nameof(HasDataElementSelection))]
    [NotifyPropertyChangedFor(nameof(HasShapeSelection))]
    [NotifyPropertyChangedFor(nameof(HasFormattingSelection))]
    private LabelElementViewModel? selectedElement;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool isDirty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    partial void OnDocumentNameChanged(string value) => MarkDirty("document:name");

    partial void OnPrinterDpiChanged(int value) => MarkDirty("document:dpi");

    partial void OnSelectedElementChanged(LabelElementViewModel? value)
    {
        if (!isLoading && !isRestoringHistory)
        {
            history.UpdateSelection(value?.Id);
        }
    }

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
        printCopies = 1;
        printPrinterName = null;
        currentPath = null;
        SelectedElement = null;
        isLoading = false;
        IsDirty = false;
        StatusMessage = "New 100 × 50 mm label";
        ResetHistory(markAsSaved: true);
    }

    [RelayCommand]
    private void AddText() => AddElementAt(LabelElementKind.Text, 5, 5, beginEditing: true);

    [RelayCommand]
    private void AddBarcode() => AddElementAt(LabelElementKind.Barcode, 5, 5);

    [RelayCommand]
    private void AddQrCode() => AddElementAt(LabelElementKind.QrCode, 5, 5);

    [RelayCommand]
    private void AddRectangle() => AddElementAt(LabelElementKind.Rectangle, 5, 5);

    [RelayCommand]
    private void AddRoundedRectangle() => AddElementAt(LabelElementKind.RoundedRectangle, 5, 5);

    [RelayCommand]
    private void AddLine() => AddElementAt(LabelElementKind.Line, 5, 5);

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

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (history.TryUndo(out var snapshot) && snapshot is not null)
        {
            RestoreSnapshot(snapshot, "Undo");
        }
    }

    private bool CanUndo() => history.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (history.TryRedo(out var snapshot) && snapshot is not null)
        {
            RestoreSnapshot(snapshot, "Redo");
        }
    }

    private bool CanRedo() => history.CanRedo;

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
            ResetHistory(markAsSaved: true);
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
            savedDocumentFingerprint = CreateFingerprint(CreateDocument());
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
        var document = CreateDocument();
        try
        {
            var printed = printService.Print(document);
            StatusMessage = printed ? "Label sent to printer" : "Printing cancelled";
        }
        catch (Exception exception) when (exception is PrintSystemException or InvalidOperationException)
        {
            StatusMessage = $"Could not print label: {exception.Message}";
        }
        finally
        {
            ApplyPrintSettings(document.PrintSettings);
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
            LabelElementKind.Rectangle => (string.Empty, 35d, 20d),
            LabelElementKind.RoundedRectangle => (string.Empty, 35d, 20d),
            LabelElementKind.Line => (string.Empty, 35d, 1d),
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
        PrintSettings = new LabelPrintSettings
        {
            Copies = Math.Clamp(printCopies, 1, 999),
            PrinterName = printPrinterName
        },
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
        var printSettings = document.PrintSettings ?? new LabelPrintSettings();
        printCopies = Math.Clamp(printSettings.Copies, 1, 999);
        printPrinterName = string.IsNullOrWhiteSpace(printSettings.PrinterName)
            ? null
            : printSettings.PrinterName;

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
        if (sender is LabelElementViewModel element &&
            e.PropertyName != nameof(LabelElementViewModel.IsEditing))
        {
            var isGeometryChange = e.PropertyName is
                nameof(LabelElementViewModel.X) or
                nameof(LabelElementViewModel.Y) or
                nameof(LabelElementViewModel.Width) or
                nameof(LabelElementViewModel.Height) or
                nameof(LabelElementViewModel.IsLineDirectionReversed);
            var mergeKey = isGeometryChange
                ? $"element:{element.Id}:geometry"
                : $"element:{element.Id}:{e.PropertyName}";
            MarkDirty(mergeKey);
        }
    }

    private void MarkDirty(string? mergeKey = null)
    {
        if (!isLoading && !isRestoringHistory)
        {
            IsDirty = true;
            history.Record(CaptureSnapshot(), mergeKey);
            NotifyHistoryCommandsChanged();
        }
    }

    private void ApplyPrintSettings(LabelPrintSettings settings)
    {
        var copies = Math.Clamp(settings.Copies, 1, 999);
        var printerName = string.IsNullOrWhiteSpace(settings.PrinterName) ? null : settings.PrinterName;
        var settingsChanged = printCopies != copies ||
            !string.Equals(printPrinterName, printerName, StringComparison.Ordinal);

        printCopies = copies;
        printPrinterName = printerName;
        if (settingsChanged)
        {
            MarkDirty("document:print-settings");
        }
    }

    private DesignerSnapshot CaptureSnapshot() =>
        new(CreateDocument(), SelectedElement?.Id);

    private void ResetHistory(bool markAsSaved)
    {
        var snapshot = CaptureSnapshot();
        history.Reset(snapshot);
        if (markAsSaved)
        {
            savedDocumentFingerprint = CreateFingerprint(snapshot.Document);
        }

        NotifyHistoryCommandsChanged();
    }

    private void RestoreSnapshot(DesignerSnapshot snapshot, string actionName)
    {
        isRestoringHistory = true;
        try
        {
            LoadDocument(snapshot.Document);
            SelectedElement = snapshot.SelectedElementId is Guid selectedId
                ? Elements.FirstOrDefault(element => element.Id == selectedId)
                : null;
        }
        finally
        {
            isRestoringHistory = false;
        }

        IsDirty = !string.Equals(
            CreateFingerprint(CreateDocument()),
            savedDocumentFingerprint,
            StringComparison.Ordinal);
        StatusMessage = actionName;
        NotifyHistoryCommandsChanged();
    }

    private void NotifyHistoryCommandsChanged()
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private static string CreateFingerprint(LabelDocument document) =>
        JsonSerializer.Serialize(document);

    private static string MakeSafeFileName(string name)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeName = new string(name.Where(character => !invalidCharacters.Contains(character)).ToArray()).Trim();
        return string.IsNullOrEmpty(safeName) ? "Untitled label" : safeName;
    }

    private static double NormalizeDimension(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 1, 1000) : 1;
}
