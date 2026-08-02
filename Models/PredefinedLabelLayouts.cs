namespace wLabelDesigner.Models;

public static class PredefinedLabelLayouts
{
    public static IReadOnlyList<PredefinedLabelLayout> All { get; } =
    [
        new(
            "Shipping label",
            "Recipient, address, tracking barcode, and order reference.",
            100,
            150,
            203,
            CreateShippingLabel),
        new(
            "Product label",
            "Compact product name, price, SKU, and barcode layout.",
            50,
            30,
            203,
            CreateProductLabel),
        new(
            "Shelf label",
            "Wide shelf tag with product details and a scannable barcode.",
            100,
            50,
            203,
            CreateShelfLabel),
        new(
            "QR contact card",
            "Name and contact details paired with a prominent QR code.",
            60,
            40,
            300,
            CreateQrContactCard)
    ];

    private static LabelDocument CreateShippingLabel() => new()
    {
        Name = "Shipping label",
        WidthMillimeters = 100,
        HeightMillimeters = 150,
        PrinterDpi = 203,
        Elements =
        [
            Text("SHIP TO", 6, 7, 88, 10, 16, bold: true, alignment: TextAlignmentOption.Left),
            Text("Recipient name", 6, 21, 88, 10, 15, bold: true, alignment: TextAlignmentOption.Left),
            Text("Street address\nCity, postal code\nCountry", 6, 33, 88, 28, 12, alignment: TextAlignmentOption.Left),
            Line(6, 66, 88),
            Text("ORDER #10001", 6, 72, 88, 9, 11, alignment: TextAlignmentOption.Left),
            Barcode("123456789012", 10, 92, 80, 28),
            Text("1234 5678 9012", 6, 123, 88, 8, 10),
            Rectangle(4, 4, 92, 140, 1.2)
        ]
    };

    private static LabelDocument CreateProductLabel() => new()
    {
        Name = "Product label",
        WidthMillimeters = 50,
        HeightMillimeters = 30,
        PrinterDpi = 203,
        Elements =
        [
            Text("PRODUCT NAME", 2, 2, 27, 6, 10, bold: true, alignment: TextAlignmentOption.Left),
            Text("$19.99", 31, 2, 17, 7, 14, bold: true),
            Barcode("12345678", 3, 11, 44, 11),
            Text("SKU 12345678", 2, 23, 46, 5, 8)
        ]
    };

    private static LabelDocument CreateShelfLabel() => new()
    {
        Name = "Shelf label",
        WidthMillimeters = 100,
        HeightMillimeters = 50,
        PrinterDpi = 203,
        Elements =
        [
            Text("PRODUCT NAME", 4, 4, 58, 9, 16, bold: true, alignment: TextAlignmentOption.Left),
            Text("Variant · 500 g", 4, 14, 58, 7, 10, alignment: TextAlignmentOption.Left),
            Text("$9.99", 67, 4, 29, 15, 24, bold: true),
            Barcode("123456789012", 10, 27, 80, 15),
            Text("123456789012", 4, 43, 92, 5, 8)
        ]
    };

    private static LabelDocument CreateQrContactCard() => new()
    {
        Name = "QR contact card",
        WidthMillimeters = 60,
        HeightMillimeters = 40,
        PrinterDpi = 300,
        Elements =
        [
            Text("YOUR NAME", 3, 5, 31, 8, 14, bold: true, alignment: TextAlignmentOption.Left),
            Text("Role or company", 3, 14, 31, 6, 9, alignment: TextAlignmentOption.Left),
            Text("email@example.com\n+00 000 000 000", 3, 23, 31, 12, 8, alignment: TextAlignmentOption.Left),
            QrCode("https://example.com", 37, 7, 20)
        ]
    };

    private static LabelElementData Text(
        string content,
        double x,
        double y,
        double width,
        double height,
        double fontSize,
        bool bold = false,
        TextAlignmentOption alignment = TextAlignmentOption.Center) => new()
        {
            Kind = LabelElementKind.Text,
            Content = content,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            FontSize = fontSize,
            IsBold = bold,
            TextAlignment = alignment
        };

    private static LabelElementData Barcode(string content, double x, double y, double width, double height) => new()
    {
        Kind = LabelElementKind.Barcode,
        Content = content,
        X = x,
        Y = y,
        Width = width,
        Height = height
    };

    private static LabelElementData QrCode(string content, double x, double y, double size) => new()
    {
        Kind = LabelElementKind.QrCode,
        Content = content,
        X = x,
        Y = y,
        Width = size,
        Height = size
    };

    private static LabelElementData Line(double x, double y, double width) => new()
    {
        Kind = LabelElementKind.Line,
        X = x,
        Y = y,
        Width = width,
        Height = 0.5
    };

    private static LabelElementData Rectangle(double x, double y, double width, double height, double strokeThickness) => new()
    {
        Kind = LabelElementKind.Rectangle,
        X = x,
        Y = y,
        Width = width,
        Height = height,
        StrokeThickness = strokeThickness
    };
}
