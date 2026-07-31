using CommunityToolkit.Mvvm.ComponentModel;
using wLabelDesigner.Models;

namespace wLabelDesigner.ViewModels;

public sealed partial class LabelElementViewModel : ObservableObject
{
    public LabelElementViewModel(LabelElementData data)
    {
        Id = data.Id;
        Kind = data.Kind;
        content = data.Content;
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
    }

    public Guid Id { get; }

    public LabelElementKind Kind { get; }

    public string DisplayName => Kind switch
    {
        LabelElementKind.Text => "Text",
        LabelElementKind.Barcode => "Barcode",
        LabelElementKind.QrCode => "QR code",
        _ => Kind.ToString()
    };

    [ObservableProperty]
    private string content;

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

    public LabelElementData ToData() => new()
    {
        Id = Id,
        Kind = Kind,
        Content = Content,
        X = X,
        Y = Y,
        Width = Width,
        Height = Height,
        FontSize = FontSize,
        FontFamily = FontFamily,
        IsBold = IsBold,
        IsItalic = IsItalic,
        IsUnderlined = IsUnderlined,
        TextAlignment = TextAlignment
    };

    private static double Normalize(double value, double minimum, double maximum) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : minimum;
}
