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
        set => SetProperty(ref x, Normalize(value, 0, 1000));
    }

    private double y;

    public double Y
    {
        get => y;
        set => SetProperty(ref y, Normalize(value, 0, 1000));
    }

    private double width;

    public double Width
    {
        get => width;
        set => SetProperty(ref width, Normalize(value, 0.1, 1000));
    }

    private double height;

    public double Height
    {
        get => height;
        set => SetProperty(ref height, Normalize(value, 0.1, 1000));
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
    private bool isLineDirectionReversed;

    private double rotationDegrees;

    public double RotationDegrees
    {
        get => rotationDegrees;
        set => SetProperty(ref rotationDegrees, NormalizeRotation(value));
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
