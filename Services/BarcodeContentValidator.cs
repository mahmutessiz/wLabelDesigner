using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public static class BarcodeContentValidator
{
    private const string Code39Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    public static string? GetError(BarcodeFormatOption format, string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return "Enter barcode content.";
        }

        return format switch
        {
            BarcodeFormatOption.Code128 => ValidateCode128(content),
            BarcodeFormatOption.Code39 => ValidateCode39(content),
            BarcodeFormatOption.Ean8 => ValidateNumeric(content, 8, "EAN-8", validateCheckDigit: true),
            BarcodeFormatOption.Ean13 => ValidateNumeric(content, 13, "EAN-13", validateCheckDigit: true),
            BarcodeFormatOption.UpcA => ValidateNumeric(content, 12, "UPC-A", validateCheckDigit: true),
            BarcodeFormatOption.Itf14 => ValidateNumeric(content, 14, "ITF-14", validateCheckDigit: true),
            _ => "The selected barcode format is not supported."
        };
    }

    public static bool IsValid(BarcodeFormatOption format, string? content) =>
        GetError(format, content) is null;

    private static string? ValidateCode128(string content)
    {
        if (content.Length > 120)
        {
            return "Code 128 supports at most 120 characters.";
        }

        return content.All(character => character is >= ' ' and <= '~')
            ? null
            : "Code 128 supports printable ASCII characters only.";
    }

    private static string? ValidateCode39(string content)
    {
        if (content.Length > 80)
        {
            return "Code 39 supports at most 80 characters.";
        }

        return content.All(Code39Characters.Contains)
            ? null
            : "Code 39 supports A-Z, 0-9, space, and - . $ / + % only.";
    }

    private static string? ValidateNumeric(
        string content,
        int requiredLength,
        string formatName,
        bool validateCheckDigit)
    {
        if (content.Length != requiredLength || content.Any(character => character is < '0' or > '9'))
        {
            return $"{formatName} requires exactly {requiredLength} digits.";
        }

        if (!validateCheckDigit)
        {
            return null;
        }

        var expected = CalculateGs1CheckDigit(content.AsSpan(0, content.Length - 1));
        return content[^1] - '0' == expected
            ? null
            : $"{formatName} check digit should be {expected}.";
    }

    private static int CalculateGs1CheckDigit(ReadOnlySpan<char> digits)
    {
        var sum = 0;
        var weight = 3;
        for (var index = digits.Length - 1; index >= 0; index--)
        {
            sum += (digits[index] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        return (10 - (sum % 10)) % 10;
    }
}
