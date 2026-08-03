using System.Windows;
using System.Windows.Media;
using wLabelDesigner.Models;
using wLabelDesigner.Services;

namespace wLabelDesigner.Controls;

public sealed class LabelTextPresenter : FrameworkElement
{
    private static readonly DependencyPropertyKey HasOverflowPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasOverflow),
            typeof(bool),
            typeof(LabelTextPresenter),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HasOverflowProperty = HasOverflowPropertyKey.DependencyProperty;
    public static readonly DependencyProperty TextProperty = Register<string>(nameof(Text), string.Empty);
    public static readonly DependencyProperty TypefaceFamilyProperty = Register<string>(nameof(TypefaceFamily), "Segoe UI");
    public static readonly DependencyProperty TextFontSizeProperty = Register<double>(nameof(TextFontSize), 12d);
    public static readonly DependencyProperty IsBoldProperty = Register<bool>(nameof(IsBold), false);
    public static readonly DependencyProperty IsItalicProperty = Register<bool>(nameof(IsItalic), false);
    public static readonly DependencyProperty IsUnderlinedProperty = Register<bool>(nameof(IsUnderlined), false);
    public static readonly DependencyProperty HorizontalTextAlignmentProperty = Register<TextAlignmentOption>(nameof(HorizontalTextAlignment), TextAlignmentOption.Center);
    public static readonly DependencyProperty VerticalTextAlignmentProperty = Register<VerticalTextAlignmentOption>(nameof(VerticalTextAlignment), VerticalTextAlignmentOption.Top);
    public static readonly DependencyProperty TextColorProperty = Register<string>(nameof(TextColor), "#FF000000");
    public static readonly DependencyProperty LineSpacingProperty = Register<double>(nameof(LineSpacing), 1d);
    public static readonly DependencyProperty LetterSpacingProperty = Register<double>(nameof(LetterSpacing), 0d);
    public static readonly DependencyProperty IsAutoFitEnabledProperty = Register<bool>(nameof(IsAutoFitEnabled), false);

    public bool HasOverflow => (bool)GetValue(HasOverflowProperty);
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string TypefaceFamily { get => (string)GetValue(TypefaceFamilyProperty); set => SetValue(TypefaceFamilyProperty, value); }
    public double TextFontSize { get => (double)GetValue(TextFontSizeProperty); set => SetValue(TextFontSizeProperty, value); }
    public bool IsBold { get => (bool)GetValue(IsBoldProperty); set => SetValue(IsBoldProperty, value); }
    public bool IsItalic { get => (bool)GetValue(IsItalicProperty); set => SetValue(IsItalicProperty, value); }
    public bool IsUnderlined { get => (bool)GetValue(IsUnderlinedProperty); set => SetValue(IsUnderlinedProperty, value); }
    public TextAlignmentOption HorizontalTextAlignment { get => (TextAlignmentOption)GetValue(HorizontalTextAlignmentProperty); set => SetValue(HorizontalTextAlignmentProperty, value); }
    public VerticalTextAlignmentOption VerticalTextAlignment { get => (VerticalTextAlignmentOption)GetValue(VerticalTextAlignmentProperty); set => SetValue(VerticalTextAlignmentProperty, value); }
    public string TextColor { get => (string)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public double LineSpacing { get => (double)GetValue(LineSpacingProperty); set => SetValue(LineSpacingProperty, value); }
    public double LetterSpacing { get => (double)GetValue(LetterSpacingProperty); set => SetValue(LetterSpacingProperty, value); }
    public bool IsAutoFitEnabled { get => (bool)GetValue(IsAutoFitEnabledProperty); set => SetValue(IsAutoFitEnabledProperty, value); }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var bounds = new Rect(RenderSize);
        if (bounds.IsEmpty)
        {
            SetValue(HasOverflowPropertyKey, false);
            return;
        }

        var element = new LabelElementData
        {
            Kind = LabelElementKind.Text,
            Content = Text ?? string.Empty,
            FontFamily = TypefaceFamily,
            FontSize = TextFontSize,
            IsBold = IsBold,
            IsItalic = IsItalic,
            IsUnderlined = IsUnderlined,
            TextAlignment = HorizontalTextAlignment,
            VerticalTextAlignment = VerticalTextAlignment,
            TextColor = TextColor,
            LineSpacing = LineSpacing,
            LetterSpacing = LetterSpacing,
            IsTextAutoFitEnabled = IsAutoFitEnabled
        };
        var brush = LabelDrawingRenderer.CreateBrush(TextColor, Brushes.Black);
        drawingContext.PushClip(new RectangleGeometry(bounds));
        var result = TextLayoutEngine.Draw(drawingContext, element, bounds, brush);
        drawingContext.Pop();
        SetValue(HasOverflowPropertyKey, result.HasOverflow);
    }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(
            name,
            typeof(T),
            typeof(LabelTextPresenter),
            new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));
}
