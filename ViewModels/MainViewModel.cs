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
    private readonly IElementClipboard elementClipboard;
    private readonly UndoHistory history = new();
    private readonly List<LabelElementViewModel> selectedElements = [];
    private string? currentPath;
    private bool isLoading;
    private bool isRestoringHistory;
    private bool isSynchronizingSelection;
    private bool isApplyingBatchChange;
    private string savedDocumentFingerprint = string.Empty;
    private int printCopies = 1;
    private string? printPrinterName;
    private int pasteSequence = 1;

    public MainViewModel(
        ILabelDocumentStore documentStore,
        IFileDialogService fileDialogService,
        ILabelPrintService printService,
        IElementClipboard elementClipboard)
    {
        this.documentStore = documentStore;
        this.fileDialogService = fileDialogService;
        this.printService = printService;
        this.elementClipboard = elementClipboard;
        NewDocument();
    }

    public ObservableCollection<LabelElementViewModel> Elements { get; } = [];

    public IReadOnlyList<int> SupportedDpi { get; } = [203, 300];

    public IReadOnlyList<string> AvailableFontFamilies { get; } =
        ["Arial", "Calibri", "Consolas", "Segoe UI", "Tahoma", "Times New Roman", "Verdana"];

    public IReadOnlyList<double> CommonFontSizes { get; } = [8, 9, 10, 11, 12, 14, 16, 18, 24, 32, 48, 64, 72];

    public IReadOnlyList<double> CommonStrokeWidths { get; } = [0.5, 0.75, 1, 1.5, 2, 3, 4, 6];

    public IReadOnlyList<double> CommonGridSizes { get; } = [1, 2, 2.5, 5, 10, 20];

    public IReadOnlyList<LabelElementViewModel> SelectedElements => selectedElements;

    public IReadOnlyList<LabelElementViewModel> LayerElements => Elements.Reverse().ToArray();

    public bool HasMultipleSelection => selectedElements.Count > 1;

    public bool HasSingleSelection => selectedElements.Count == 1;

    public bool HasTextSelection => selectedElements.Count == 1 && SelectedElement?.Kind == LabelElementKind.Text;

    public bool HasDataElementSelection => selectedElements.Count == 1 &&
        SelectedElement?.Kind is LabelElementKind.Barcode or LabelElementKind.QrCode;

    public bool HasShapeSelection => selectedElements.Count == 1 && SelectedElement?.Kind is
        LabelElementKind.Rectangle or LabelElementKind.RoundedRectangle or LabelElementKind.Line;

    public bool HasFormattingSelection => HasTextSelection || HasShapeSelection || HasMultipleSelection;

    public string WindowTitle => $"{DocumentName}{(IsDirty ? " *" : string.Empty)} — wLabel Designer";

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
    [NotifyPropertyChangedFor(nameof(HasSingleSelection))]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(CutCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateCommand))]
    [NotifyCanExecuteChangedFor(nameof(NudgeCommand))]
    [NotifyCanExecuteChangedFor(nameof(PasteCommand))]
    private LabelElementViewModel? selectedElement;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool isDirty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomScale))]
    private double zoomPercent = 100;

    public double ZoomScale => ZoomPercent / 100d;

    [ObservableProperty]
    private bool isGridVisible = true;

    [ObservableProperty]
    private bool areRulersVisible = true;

    private double gridSize = 5;

    public double GridSize
    {
        get => gridSize;
        set => SetProperty(ref gridSize, NormalizeGridSize(value));
    }

    partial void OnDocumentNameChanged(string value) => MarkDirty("document:name");

    partial void OnPrinterDpiChanged(int value) => MarkDirty("document:dpi");

    partial void OnSelectedElementChanged(LabelElementViewModel? value)
    {
        if (!isSynchronizingSelection)
        {
            selectedElements.Clear();
            if (value is not null)
            {
                selectedElements.Add(value);
            }

            NotifySelectionStateChanged();
        }

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
        OnPropertyChanged(nameof(LayerElements));
        isLoading = true;
        DocumentName = "Untitled label";
        LabelWidth = 100;
        LabelHeight = 50;
        PrinterDpi = 203;
        printCopies = 1;
        printPrinterName = null;
        pasteSequence = 1;
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
        if (selectedElements.Count == 0)
        {
            return;
        }

        var elementsToDelete = selectedElements.Where(element => !element.IsLocked).ToArray();
        if (elementsToDelete.Length == 0)
        {
            return;
        }
        foreach (var element in elementsToDelete)
        {
            element.PropertyChanged -= OnElementPropertyChanged;
            Elements.Remove(element);
        }

        SelectedElement = null;
        OnPropertyChanged(nameof(LayerElements));
        MarkDirty();
        StatusMessage = elementsToDelete.Length == 1
            ? "Element deleted"
            : $"{elementsToDelete.Length} elements deleted";
    }

    private bool CanDeleteSelected() => selectedElements.Any(element => !element.IsLocked);

    [RelayCommand(CanExecute = nameof(CanDeselectAll))]
    private void DeselectAll()
    {
        SelectedElement = null;
        StatusMessage = "Selection cleared";
    }

    private bool CanDeselectAll() => selectedElements.Count > 0;

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
    private void ZoomIn() => SetZoom(NextZoomLevel(ZoomPercent, increase: true));

    [RelayCommand]
    private void ZoomOut() => SetZoom(NextZoomLevel(ZoomPercent, increase: false));

    [RelayCommand]
    private void ResetZoom() => SetZoom(100);

    public void SetZoom(double percent)
    {
        ZoomPercent = Math.Clamp(Math.Round(percent), 25, 400);
        StatusMessage = $"Zoom {ZoomPercent:0}%";
    }

    public void SetSelectionFromView(
        IEnumerable<LabelElementViewModel> elements,
        LabelElementViewModel? primaryElement)
    {
        var selection = elements.Distinct().ToArray();
        isSynchronizingSelection = true;
        try
        {
            selectedElements.Clear();
            selectedElements.AddRange(selection);
            SelectedElement = primaryElement is not null && selection.Contains(primaryElement)
                ? primaryElement
                : selection.LastOrDefault();
        }
        finally
        {
            isSynchronizingSelection = false;
        }

        history.UpdateSelection(SelectedElement?.Id);
        NotifySelectionStateChanged();
    }

    public void MoveSelectedElements(double horizontalChange, double verticalChange)
    {
        var movableElements = selectedElements.Where(element => !element.IsLocked).ToArray();
        if (movableElements.Length == 0 ||
            (!double.IsFinite(horizontalChange) && !double.IsFinite(verticalChange)))
        {
            return;
        }

        var minimumX = movableElements.Min(element => element.X);
        var minimumY = movableElements.Min(element => element.Y);
        var maximumRight = movableElements.Max(element => element.X + element.Width);
        var maximumBottom = movableElements.Max(element => element.Y + element.Height);
        var boundedHorizontalChange = Math.Clamp(
            double.IsFinite(horizontalChange) ? horizontalChange : 0,
            -minimumX,
            LabelWidth - maximumRight);
        var boundedVerticalChange = Math.Clamp(
            double.IsFinite(verticalChange) ? verticalChange : 0,
            -minimumY,
            LabelHeight - maximumBottom);

        if (boundedHorizontalChange == 0 && boundedVerticalChange == 0)
        {
            return;
        }

        ApplyBatchChange(() =>
        {
            foreach (var element in movableElements)
            {
                element.X += boundedHorizontalChange;
                element.Y += boundedVerticalChange;
            }
        }, "selection:geometry");
    }

    [RelayCommand(CanExecute = nameof(CanAlignSelection))]
    private void AlignSelection(string? alignment)
    {
        if (selectedElements.Count < 2 || selectedElements.Any(element => element.IsLocked) || string.IsNullOrWhiteSpace(alignment))
        {
            return;
        }

        var left = selectedElements.Min(element => element.X);
        var top = selectedElements.Min(element => element.Y);
        var right = selectedElements.Max(element => element.X + element.Width);
        var bottom = selectedElements.Max(element => element.Y + element.Height);
        var horizontalCenter = (left + right) / 2;
        var verticalCenter = (top + bottom) / 2;

        ApplyBatchChange(() =>
        {
            foreach (var element in selectedElements)
            {
                switch (alignment)
                {
                    case "Left": element.X = left; break;
                    case "HorizontalCenter": element.X = horizontalCenter - (element.Width / 2); break;
                    case "Right": element.X = right - element.Width; break;
                    case "Top": element.Y = top; break;
                    case "VerticalCenter": element.Y = verticalCenter - (element.Height / 2); break;
                    case "Bottom": element.Y = bottom - element.Height; break;
                }
            }
        }, null);
        StatusMessage = $"Aligned {selectedElements.Count} elements";
    }

    private bool CanAlignSelection(string? alignment) =>
        selectedElements.Count >= 2 && selectedElements.All(element => !element.IsLocked);

    [RelayCommand(CanExecute = nameof(CanDistributeSelection))]
    private void DistributeSelection(string? direction)
    {
        if (selectedElements.Count < 3 || selectedElements.Any(element => element.IsLocked) || string.IsNullOrWhiteSpace(direction))
        {
            return;
        }

        ApplyBatchChange(() =>
        {
            if (direction == "Horizontal")
            {
                var ordered = selectedElements.OrderBy(element => element.X).ToArray();
                var availableSpan = (ordered[^1].X + ordered[^1].Width) - ordered[0].X;
                var totalWidth = ordered.Sum(element => element.Width);
                var gap = (availableSpan - totalWidth) / (ordered.Length - 1);
                var position = ordered[0].X + ordered[0].Width + gap;
                for (var index = 1; index < ordered.Length - 1; index++)
                {
                    ordered[index].X = position;
                    position += ordered[index].Width + gap;
                }
            }
            else
            {
                var ordered = selectedElements.OrderBy(element => element.Y).ToArray();
                var availableSpan = (ordered[^1].Y + ordered[^1].Height) - ordered[0].Y;
                var totalHeight = ordered.Sum(element => element.Height);
                var gap = (availableSpan - totalHeight) / (ordered.Length - 1);
                var position = ordered[0].Y + ordered[0].Height + gap;
                for (var index = 1; index < ordered.Length - 1; index++)
                {
                    ordered[index].Y = position;
                    position += ordered[index].Height + gap;
                }
            }
        }, null);
        StatusMessage = $"Distributed {selectedElements.Count} elements";
    }

    private bool CanDistributeSelection(string? direction) =>
        selectedElements.Count >= 3 && selectedElements.All(element => !element.IsLocked);

    [RelayCommand(CanExecute = nameof(CanMoveLayerForward))]
    private void BringForward() => MoveSelectedLayer(Elements.IndexOf(SelectedElement!) + 1, "Brought element forward");

    private bool CanMoveLayerForward() =>
        SelectedElement is { IsLocked: false } element && Elements.IndexOf(element) < Elements.Count - 1;

    [RelayCommand(CanExecute = nameof(CanMoveLayerBackward))]
    private void SendBackward() => MoveSelectedLayer(Elements.IndexOf(SelectedElement!) - 1, "Sent element backward");

    private bool CanMoveLayerBackward() =>
        SelectedElement is { IsLocked: false } element && Elements.IndexOf(element) > 0;

    [RelayCommand(CanExecute = nameof(CanMoveLayerForward))]
    private void BringToFront() => MoveSelectedLayer(Elements.Count - 1, "Brought element to front");

    [RelayCommand(CanExecute = nameof(CanMoveLayerBackward))]
    private void SendToBack() => MoveSelectedLayer(0, "Sent element to back");

    [RelayCommand(CanExecute = nameof(CanManipulateSelectedElement))]
    private void Copy()
    {
        if (TryCopySelectedElement())
        {
            pasteSequence = 1;
            PasteCommand.NotifyCanExecuteChanged();
            StatusMessage = "Element copied";
        }
        else
        {
            StatusMessage = "Could not access the Windows clipboard";
        }
    }

    [RelayCommand(CanExecute = nameof(CanManipulateSelectedElement))]
    private void Cut()
    {
        if (!TryCopySelectedElement())
        {
            StatusMessage = "Could not access the Windows clipboard";
            return;
        }

        DeleteSelected();
        pasteSequence = 1;
        PasteCommand.NotifyCanExecuteChanged();
        StatusMessage = "Element cut";
    }

    [RelayCommand(CanExecute = nameof(CanPaste))]
    private void Paste()
    {
        if (!elementClipboard.TryGetElement(out var source) || source is null)
        {
            StatusMessage = "Could not read an element from the Windows clipboard";
            return;
        }

        if (!Enum.IsDefined(source.Kind))
        {
            StatusMessage = "The clipboard contains an unsupported element type";
            return;
        }

        var offset = Math.Min(30, pasteSequence * 3d);
        var element = AddElementData(CloneElement(source, offset), "Element pasted");
        SelectedElement = element;
        pasteSequence++;
    }

    private bool CanPaste() =>
        SelectedElement?.IsEditing != true && elementClipboard.ContainsElement();

    [RelayCommand(CanExecute = nameof(CanManipulateSelectedElement))]
    private void Duplicate()
    {
        if (SelectedElement is null)
        {
            return;
        }

        var element = AddElementData(CloneElement(SelectedElement.ToData(), offset: 3), "Element duplicated");
        SelectedElement = element;
    }

    private bool CanManipulateSelectedElement() =>
        selectedElements.Count == 1 && SelectedElement is { IsEditing: false, IsLocked: false };

    [RelayCommand(CanExecute = nameof(CanNudge))]
    private void Nudge(string? direction)
    {
        if (SelectedElement is null || string.IsNullOrWhiteSpace(direction))
        {
            return;
        }

        var parts = direction.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var step = parts.Length > 1 && string.Equals(parts[1], "Large", StringComparison.OrdinalIgnoreCase)
            ? 5d
            : 0.5d;

        var horizontalChange = 0d;
        var verticalChange = 0d;
        switch (parts[0])
        {
            case "Left": horizontalChange = -step; break;
            case "Right": horizontalChange = step; break;
            case "Up": verticalChange = -step; break;
            case "Down": verticalChange = step; break;
        }

        MoveSelectedElements(horizontalChange, verticalChange);
        StatusMessage = parts.Length > 1 ? "Element moved 5 mm" : "Element moved 0.5 mm";
    }

    private bool CanNudge(string? direction) =>
        selectedElements.Count > 0 && selectedElements.All(element => !element.IsEditing && !element.IsLocked);

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

        var data = new LabelElementData
        {
            Kind = kind,
            Content = content,
            X = Math.Clamp(x, 0, Math.Max(0, LabelWidth - width)),
            Y = Math.Clamp(y, 0, Math.Max(0, LabelHeight - height)),
            Width = width,
            Height = height
        };
        var element = AddElementData(data, $"{kind switch
        {
            LabelElementKind.QrCode => "QR code",
            LabelElementKind.RoundedRectangle => "Rounded box",
            LabelElementKind.Rectangle => "Box",
            _ => kind.ToString()
        }} added");
        element.IsEditing = beginEditing;
        return element;
    }

    private LabelElementViewModel AddElementData(LabelElementData data, string statusMessage)
    {
        var element = new LabelElementViewModel(data)
        {
            IsEditing = false
        };

        element.PropertyChanged += OnElementPropertyChanged;
        Elements.Add(element);
        SelectedElement = element;
        OnPropertyChanged(nameof(LayerElements));
        MarkDirty();
        StatusMessage = statusMessage;
        return element;
    }

    private bool TryCopySelectedElement() =>
        SelectedElement is not null && elementClipboard.TryCopy(SelectedElement.ToData());

    private LabelElementData CloneElement(LabelElementData source, double offset)
    {
        var sourceWidth = double.IsFinite(source.Width) ? source.Width : 10;
        var sourceHeight = double.IsFinite(source.Height) ? source.Height : 10;
        var sourceX = double.IsFinite(source.X) ? source.X : 0;
        var sourceY = double.IsFinite(source.Y) ? source.Y : 0;
        var width = Math.Clamp(sourceWidth, 0.1, LabelWidth);
        var height = Math.Clamp(sourceHeight, 0.1, LabelHeight);
        return new LabelElementData
        {
            Id = Guid.NewGuid(),
            Kind = source.Kind,
            Content = source.Content ?? string.Empty,
            X = Math.Clamp(sourceX + offset, 0, Math.Max(0, LabelWidth - width)),
            Y = Math.Clamp(sourceY + offset, 0, Math.Max(0, LabelHeight - height)),
            Width = width,
            Height = height,
            FontSize = source.FontSize,
            FontFamily = string.IsNullOrWhiteSpace(source.FontFamily) ? "Segoe UI" : source.FontFamily,
            IsBold = source.IsBold,
            IsItalic = source.IsItalic,
            IsUnderlined = source.IsUnderlined,
            TextAlignment = source.TextAlignment,
            StrokeThickness = source.StrokeThickness,
            IsLineDirectionReversed = source.IsLineDirectionReversed,
            RotationDegrees = source.RotationDegrees,
            IsLocked = source.IsLocked,
            IsVisible = source.IsVisible
        };
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

        OnPropertyChanged(nameof(LayerElements));

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
        if (isApplyingBatchChange)
        {
            return;
        }

        if (e.PropertyName == nameof(LabelElementViewModel.IsEditing))
        {
            NotifySelectionCommandsChanged();
            return;
        }

        if (sender is LabelElementViewModel element)
        {
            if (e.PropertyName == nameof(LabelElementViewModel.IsLocked))
            {
                NotifySelectionStateChanged();
            }

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

    private void NotifySelectionCommandsChanged()
    {
        CopyCommand.NotifyCanExecuteChanged();
        CutCommand.NotifyCanExecuteChanged();
        PasteCommand.NotifyCanExecuteChanged();
        DuplicateCommand.NotifyCanExecuteChanged();
        NudgeCommand.NotifyCanExecuteChanged();
    }

    private void NotifySelectionStateChanged()
    {
        OnPropertyChanged(nameof(SelectedElements));
        OnPropertyChanged(nameof(HasMultipleSelection));
        OnPropertyChanged(nameof(HasSingleSelection));
        OnPropertyChanged(nameof(HasTextSelection));
        OnPropertyChanged(nameof(HasDataElementSelection));
        OnPropertyChanged(nameof(HasShapeSelection));
        OnPropertyChanged(nameof(HasFormattingSelection));
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        DeselectAllCommand.NotifyCanExecuteChanged();
        AlignSelectionCommand.NotifyCanExecuteChanged();
        DistributeSelectionCommand.NotifyCanExecuteChanged();
        NotifySelectionCommandsChanged();
        NotifyLayerCommandsChanged();
    }

    private void ApplyBatchChange(Action change, string? mergeKey)
    {
        isApplyingBatchChange = true;
        try
        {
            change();
        }
        finally
        {
            isApplyingBatchChange = false;
        }

        MarkDirty(mergeKey);
    }

    private void MoveSelectedLayer(int targetIndex, string statusMessage)
    {
        if (SelectedElement is not { IsLocked: false } element)
        {
            return;
        }

        var currentIndex = Elements.IndexOf(element);
        targetIndex = Math.Clamp(targetIndex, 0, Elements.Count - 1);
        if (currentIndex == targetIndex)
        {
            return;
        }

        Elements.Move(currentIndex, targetIndex);
        OnPropertyChanged(nameof(LayerElements));
        MarkDirty("layers:order");
        StatusMessage = statusMessage;
        NotifyLayerCommandsChanged();
    }

    private void NotifyLayerCommandsChanged()
    {
        BringForwardCommand.NotifyCanExecuteChanged();
        SendBackwardCommand.NotifyCanExecuteChanged();
        BringToFrontCommand.NotifyCanExecuteChanged();
        SendToBackCommand.NotifyCanExecuteChanged();
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

    private static double NormalizeGridSize(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0.5, 100) : 5;

    private static double NextZoomLevel(double current, bool increase)
    {
        double[] levels = [25, 33, 50, 67, 75, 100, 125, 150, 200, 300, 400];
        return increase
            ? levels.FirstOrDefault(level => level > current, 400)
            : levels.LastOrDefault(level => level < current, 25);
    }
}
