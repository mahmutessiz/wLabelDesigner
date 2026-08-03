using System.Globalization;
using System.Windows;
using System.Windows.Media;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public readonly record struct TextRenderResult(bool HasOverflow, double EffectiveFontSize);

public static class TextLayoutEngine
{
    public static TextRenderResult Draw(
        DrawingContext drawingContext,
        LabelElementData element,
        Rect bounds,
        Brush brush)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(brush);

        var layout = CreateLayout(element, bounds.Size);
        var top = element.VerticalTextAlignment switch
        {
            VerticalTextAlignmentOption.Center => bounds.Top + Math.Max(0, (bounds.Height - layout.TotalHeight) / 2),
            VerticalTextAlignmentOption.Bottom => bounds.Bottom - layout.TotalHeight,
            _ => bounds.Top
        };

        for (var lineIndex = 0; lineIndex < layout.Lines.Count; lineIndex++)
        {
            var line = layout.Lines[lineIndex];
            var left = element.TextAlignment switch
            {
                TextAlignmentOption.Center => bounds.Left + ((bounds.Width - line.Width) / 2),
                TextAlignmentOption.Right => bounds.Right - line.Width,
                _ => bounds.Left
            };
            var x = left;
            foreach (var glyph in line.Glyphs)
            {
                if (x != left)
                {
                    x += layout.LetterSpacing;
                }

                var text = CreateFormattedText(glyph.Text, element, layout.FontSize, brush);
                drawingContext.DrawText(text, new Point(x, top + (lineIndex * layout.LineAdvance)));
                x += glyph.Width;
            }
        }

        return new TextRenderResult(layout.HasOverflow, layout.FontSize);
    }

    public static TextRenderResult Measure(LabelElementData element, Size availableSize)
    {
        ArgumentNullException.ThrowIfNull(element);
        var layout = CreateLayout(element, availableSize);
        return new TextRenderResult(layout.HasOverflow, layout.FontSize);
    }

    private static TextLayout CreateLayout(LabelElementData element, Size availableSize)
    {
        var width = double.IsFinite(availableSize.Width) ? Math.Max(0, availableSize.Width) : 0;
        var height = double.IsFinite(availableSize.Height) ? Math.Max(0, availableSize.Height) : 0;
        var requestedSize = double.IsFinite(element.FontSize) ? Math.Clamp(element.FontSize, 1, 512) : 12;
        var requested = LayoutAtSize(element, width, height, requestedSize);
        if (!element.IsTextAutoFitEnabled || !requested.HasOverflow || requestedSize <= 1)
        {
            return requested;
        }

        var minimum = LayoutAtSize(element, width, height, 1);
        if (minimum.HasOverflow)
        {
            return minimum;
        }

        var lower = 1d;
        var upper = requestedSize;
        var best = minimum;
        for (var index = 0; index < 14; index++)
        {
            var candidateSize = (lower + upper) / 2;
            var candidate = LayoutAtSize(element, width, height, candidateSize);
            if (candidate.HasOverflow)
            {
                upper = candidateSize;
            }
            else
            {
                lower = candidateSize;
                best = candidate;
            }
        }

        return best;
    }

    private static TextLayout LayoutAtSize(
        LabelElementData element,
        double availableWidth,
        double availableHeight,
        double fontSize)
    {
        var lineSpacing = double.IsFinite(element.LineSpacing) ? Math.Clamp(element.LineSpacing, 0.5, 5) : 1;
        var letterSpacing = double.IsFinite(element.LetterSpacing) ? Math.Clamp(element.LetterSpacing, -5, 50) : 0;
        var measurementBrush = Brushes.Black;
        var sample = CreateFormattedText("Mg", element, fontSize, measurementBrush);
        var naturalLineHeight = Math.Max(1, sample.Height);
        var lineAdvance = naturalLineHeight * lineSpacing;
        var lines = new List<TextLineLayout>();
        var glyphs = new List<TextGlyph>();
        var lineWidth = 0d;
        var horizontalOverflow = false;

        void FinishLine()
        {
            lines.Add(new TextLineLayout([.. glyphs], Math.Max(0, lineWidth)));
            glyphs.Clear();
            lineWidth = 0;
        }

        var content = (element.Content ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var enumerator = StringInfo.GetTextElementEnumerator(content);
        while (enumerator.MoveNext())
        {
            var textElement = enumerator.GetTextElement();
            if (textElement == "\n")
            {
                FinishLine();
                continue;
            }

            var formatted = CreateFormattedText(textElement, element, fontSize, measurementBrush);
            var glyphWidth = Math.Max(0, formatted.WidthIncludingTrailingWhitespace);
            var spacing = glyphs.Count == 0 ? 0 : letterSpacing;
            if (glyphs.Count > 0 && lineWidth + spacing + glyphWidth > availableWidth)
            {
                FinishLine();
                spacing = 0;
            }

            if (glyphs.Count == 0 && glyphWidth > availableWidth)
            {
                horizontalOverflow = true;
            }

            glyphs.Add(new TextGlyph(textElement, glyphWidth));
            lineWidth += spacing + glyphWidth;
        }

        FinishLine();
        var totalHeight = naturalLineHeight + (Math.Max(0, lines.Count - 1) * lineAdvance);
        return new TextLayout(
            lines,
            fontSize,
            letterSpacing,
            lineAdvance,
            totalHeight,
            horizontalOverflow || totalHeight > availableHeight + 0.01);
    }

    private static FormattedText CreateFormattedText(
        string text,
        LabelElementData element,
        double fontSize,
        Brush brush)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                new FontFamily(string.IsNullOrWhiteSpace(element.FontFamily) ? "Segoe UI" : element.FontFamily),
                element.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                element.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal),
            fontSize,
            brush,
            pixelsPerDip: 1);
        if (element.IsUnderlined)
        {
            formattedText.SetTextDecorations(TextDecorations.Underline);
        }

        return formattedText;
    }

    private sealed record TextGlyph(string Text, double Width);

    private sealed record TextLineLayout(IReadOnlyList<TextGlyph> Glyphs, double Width);

    private sealed record TextLayout(
        IReadOnlyList<TextLineLayout> Lines,
        double FontSize,
        double LetterSpacing,
        double LineAdvance,
        double TotalHeight,
        bool HasOverflow);
}
