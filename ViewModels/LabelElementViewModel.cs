using CommunityToolkit.Mvvm.ComponentModel;
using wLabelDesigner.Models;
using wLabelDesigner.Services;

namespace wLabelDesigner.ViewModels;

public sealed partial class LabelElementViewModel : ObservableObject
{
    private ILanguageService? languageService;

    public LabelElementViewModel(LabelElementData data)
    {
        Id = data.Id;
        Kind = data.Kind;
        content = data.Content;
        barcodeFormat = Enum.IsDefined(data.BarcodeFormat)
            ? data.BarcodeFormat
            : BarcodeFormatOption.Code128;
        isBarcodeTextVisible = data.IsBarcodeTextVisible;
        barcodeQuietZoneMillimeters = NormalizeQuietZone(data.BarcodeQuietZoneMillimeters);
        qrErrorCorrection = Enum.IsDefined(data.QrErrorCorrection)
            ? data.QrErrorCorrection
            : QrErrorCorrectionOption.Medium;
        qrMarginModules = Math.Clamp(data.QrMarginModules, 0, 16);
        x = data.X;
        y = data.Y;
        width = data.Width;
        height = data.Height;
        fontSize = data.FontSize;
        fontFamily = string.IsNullOrWhiteSpace(data.FontFamily) ? "Segoe UI" : data.FontFamily;
        isBold = data.IsBold;
        isItalic = data.IsItalic;
        isUnderlined = data.IsUnderlined;
        textAlignment = data.TextAlignment;
        verticalTextAlignment = Enum.IsDefined(data.VerticalTextAlignment)
            ? data.VerticalTextAlignment
            : VerticalTextAlignmentOption.Top;
        textColor = NormalizeColor(data.TextColor, "#FF000000");
        fillColor = NormalizeColor(data.FillColor, "#00FFFFFF");
        strokeColor = NormalizeColor(data.StrokeColor, "#FF000000");
        strokeThickness = double.IsFinite(data.StrokeThickness)
            ? Math.Clamp(data.StrokeThickness, 0.25, 20)
            : 1;
        strokeStyle = Enum.IsDefined(data.StrokeStyle) ? data.StrokeStyle : StrokeStyleOption.Solid;
        opacity = double.IsFinite(data.Opacity) ? Math.Clamp(data.Opacity, 0, 1) : 1;
        cornerRadius = Normalize(data.CornerRadius, 0, 1000);
        lineSpacing = double.IsFinite(data.LineSpacing) ? Math.Clamp(data.LineSpacing, 0.5, 5) : 1;
        letterSpacing = double.IsFinite(data.LetterSpacing) ? Math.Clamp(data.LetterSpacing, -5, 50) : 0;
        isTextAutoFitEnabled = data.IsTextAutoFitEnabled;
        isLineDirectionReversed = data.IsLineDirectionReversed;
        rotationDegrees = NormalizeRotation(data.RotationDegrees);
        isLocked = data.IsLocked;
        isVisible = data.IsVisible;
    }

    public Guid Id { get; }

    public LabelElementKind Kind { get; }

    private string? localizedDisplayName;

    public string DisplayName => localizedDisplayName ?? GetDefaultDisplayName();

    private string GetDefaultDisplayName() => Kind switch
    {
        LabelElementKind.Text => "Text",
        LabelElementKind.Barcode => "Barcode",
        LabelElementKind.QrCode => "QR code",
        LabelElementKind.Rectangle => "Box",
        LabelElementKind.RoundedRectangle => "Rounded box",
        LabelElementKind.Line => "Line",
        LabelElementKind.Image => "Image",
        LabelElementKind.Ellipse => "Ellipse",
        LabelElementKind.Triangle => "Triangle",
        LabelElementKind.Diamond => "Diamond",
        _ => Kind.ToString()
    };

    public void SetLanguage(ILanguageService? languageService)
    {
        this.languageService = languageService;
        localizedDisplayName = languageService?.Translate(GetDefaultDisplayName());
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(BarcodeValidationError));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BarcodeValidationError))]
    [NotifyPropertyChangedFor(nameof(IsBarcodeContentValid))]
    private string content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BarcodeValidationError))]
    [NotifyPropertyChangedFor(nameof(IsBarcodeContentValid))]
    private BarcodeFormatOption barcodeFormat;

    [ObservableProperty]
    private bool isBarcodeTextVisible = true;

    private double barcodeQuietZoneMillimeters = 2;

    public double BarcodeQuietZoneMillimeters
    {
        get => barcodeQuietZoneMillimeters;
        set => SetProperty(ref barcodeQuietZoneMillimeters, NormalizeQuietZone(value));
    }

    [ObservableProperty]
    private QrErrorCorrectionOption qrErrorCorrection = QrErrorCorrectionOption.Medium;

    private int qrMarginModules = 4;

    public int QrMarginModules
    {
        get => qrMarginModules;
        set => SetProperty(ref qrMarginModules, Math.Clamp(value, 0, 16));
    }

    public string? BarcodeValidationError
    {
        get
        {
            if (Kind != LabelElementKind.Barcode)
            {
                return null;
            }

            var error = BarcodeContentValidator.GetError(BarcodeFormat, Content);
            return error is null ? null : languageService?.Translate(error) ?? error;
        }
    }

    public bool IsBarcodeContentValid => BarcodeValidationError is null;

    [ObservableProperty]
    private bool isEditing;

    private double x;

    public double X
    {
        get => x;
        // A rotated line may have its unrotated box outside the label even when
        // both visible endpoints are inside. Movement constrains those endpoints.
        set { if (SetProperty(ref x, Normalize(value, Kind == LabelElementKind.Line ? -1000 : 0, 1000))) OnPropertyChanged(nameof(CanvasX)); }
    }

    private double y;

    public double Y
    {
        get => y;
        set { if (SetProperty(ref y, Normalize(value, Kind == LabelElementKind.Line ? -1000 : 0, 1000))) OnPropertyChanged(nameof(CanvasY)); }
    }

    private double width;

    public double Width
    {
        get => width;
        set
        {
            if (SetProperty(ref width, Normalize(value, Kind == LabelElementKind.Line ? 0 : 0.1, 1000)))
            {
                OnPropertyChanged(nameof(CanvasWidth));
                OnPropertyChanged(nameof(DisplayRotationDegrees));
            }
        }
    }

    private double height;

    public double Height
    {
        get => height;
        set
        {
            if (SetProperty(ref height, Normalize(value, Kind == LabelElementKind.Line ? 0 : 0.1, 1000)))
            {
                OnPropertyChanged(nameof(CanvasHeight));
                OnPropertyChanged(nameof(DisplayRotationDegrees));
            }
        }
    }

    // A line's endpoint bounds can have zero area. Reserve drawing room around
    // them in the designer only; serialization and output retain exact geometry.
    private double CanvasPaddingMillimeters => Kind == LabelElementKind.Line ? 12 * 25.4 / 96 : 0;
    public double CanvasX => X - CanvasPaddingMillimeters;
    public double CanvasY => Y - CanvasPaddingMillimeters;
    public double CanvasWidth => Width + 2 * CanvasPaddingMillimeters;
    public double CanvasHeight => Height + 2 * CanvasPaddingMillimeters;

    public DesignerBounds GetMovementBounds()
    {
        if (Kind != LabelElementKind.Line) return new(X, Y, Width, Height);
        var (start, end) = LineGeometry.GetEndpoints(ToData());
        return new(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
    }

    private double fontSize;

    public double FontSize
    {
        get => fontSize;
        set => SetProperty(ref fontSize, Normalize(value, 1, 512));
    }

    [ObservableProperty]
    private string fontFamily;

    [ObservableProperty]
    private bool isBold;

    [ObservableProperty]
    private bool isItalic;

    [ObservableProperty]
    private bool isUnderlined;

    [ObservableProperty]
    private TextAlignmentOption textAlignment;

    [ObservableProperty]
    private VerticalTextAlignmentOption verticalTextAlignment;

    private string textColor;

    public string TextColor
    {
        get => textColor;
        set => SetProperty(ref textColor, NormalizeColor(value, "#FF000000"));
    }

    private string fillColor;

    public string FillColor
    {
        get => fillColor;
        set => SetProperty(ref fillColor, NormalizeColor(value, "#00FFFFFF"));
    }

    private string strokeColor;

    public string StrokeColor
    {
        get => strokeColor;
        set => SetProperty(ref strokeColor, NormalizeColor(value, "#FF000000"));
    }

    private double strokeThickness;

    public double StrokeThickness
    {
        get => strokeThickness;
        set => SetProperty(ref strokeThickness, Normalize(value, 0.25, 20));
    }

    [ObservableProperty]
    private StrokeStyleOption strokeStyle;

    private double opacity;

    public double Opacity
    {
        get => opacity;
        set => SetProperty(ref opacity, double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 1);
    }

    private double cornerRadius;

    public double CornerRadius
    {
        get => cornerRadius;
        set => SetProperty(ref cornerRadius, Normalize(value, 0, 1000));
    }

    private double lineSpacing;

    public double LineSpacing
    {
        get => lineSpacing;
        set => SetProperty(ref lineSpacing, double.IsFinite(value) ? Math.Clamp(value, 0.5, 5) : 1);
    }

    private double letterSpacing;

    public double LetterSpacing
    {
        get => letterSpacing;
        set => SetProperty(ref letterSpacing, double.IsFinite(value) ? Math.Clamp(value, -5, 50) : 0);
    }

    [ObservableProperty]
    private bool isTextAutoFitEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayRotationDegrees))]
    private bool isLineDirectionReversed;

    private double rotationDegrees;

    public double RotationDegrees
    {
        get => rotationDegrees;
        set
        {
            if (SetProperty(ref rotationDegrees, NormalizeRotation(value)))
            {
                OnPropertyChanged(nameof(DisplayRotationDegrees));
            }
        }
    }

    // Lines can be angled by moving endpoints as well as by rotating their box.
    // Show the actual line direction, while retaining the stored box transform.
    private double EndpointAngle => Kind == LabelElementKind.Line
        ? Math.Atan2(IsLineDirectionReversed ? -Height : Height, Width) * 180 / Math.PI
        : 0;

    public double DisplayRotationDegrees
    {
        get => NormalizeRotation(RotationDegrees + EndpointAngle);
        set
        {
            if (double.IsFinite(value)) RotationDegrees = NormalizeRotation(value) - EndpointAngle;
        }
    }

    [ObservableProperty]
    private bool isLocked;

    [ObservableProperty]
    private bool isVisible = true;

    public LabelElementData ToData() => new()
    {
        Id = Id,
        Kind = Kind,
        Content = Content,
        BarcodeFormat = BarcodeFormat,
        IsBarcodeTextVisible = IsBarcodeTextVisible,
        BarcodeQuietZoneMillimeters = BarcodeQuietZoneMillimeters,
        QrErrorCorrection = QrErrorCorrection,
        QrMarginModules = QrMarginModules,
        X = X,
        Y = Y,
        Width = Width,
        Height = Height,
        FontSize = FontSize,
        FontFamily = FontFamily,
        IsBold = IsBold,
        IsItalic = IsItalic,
        IsUnderlined = IsUnderlined,
        TextAlignment = TextAlignment,
        VerticalTextAlignment = VerticalTextAlignment,
        TextColor = TextColor,
        FillColor = FillColor,
        StrokeColor = StrokeColor,
        StrokeThickness = StrokeThickness,
        StrokeStyle = StrokeStyle,
        Opacity = Opacity,
        CornerRadius = CornerRadius,
        LineSpacing = LineSpacing,
        LetterSpacing = LetterSpacing,
        IsTextAutoFitEnabled = IsTextAutoFitEnabled,
        IsLineDirectionReversed = IsLineDirectionReversed,
        RotationDegrees = RotationDegrees,
        IsLocked = IsLocked,
        IsVisible = IsVisible
    };

    private static double Normalize(double value, double minimum, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : minimum;

    private static double NormalizeQuietZone(double value) =>
        double.IsFinite(value) ? Math.Round(Math.Clamp(value, 0, 25), 2) : 2;

    private static string NormalizeColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var color = value.Trim().ToUpperInvariant();
        if (color.Length == 7)
        {
            color = $"#FF{color[1..]}";
        }

        return color.Length == 9 && color[0] == '#' && color[1..].All(Uri.IsHexDigit)
            ? color
            : fallback;
    }

    private static double NormalizeRotation(double value)
    {
        if (!double.IsFinite(value))
        {
            return 0;
        }

        var normalized = value % 360;
        if (normalized > 180)
        {
            normalized -= 360;
        }
        else if (normalized < -180)
        {
            normalized += 360;
        }

        return Math.Round(normalized, 2);
    }
}
