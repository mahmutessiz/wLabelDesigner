using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace wLabelDesigner.Services;

public sealed class WpfLanguageService : ILanguageService
{
    private static readonly DependencyProperty OriginalTextProperty = DependencyProperty.RegisterAttached(
        "OriginalText",
        typeof(string),
        typeof(WpfLanguageService));
    private static readonly DependencyProperty OriginalToolTipProperty = DependencyProperty.RegisterAttached(
        "OriginalToolTip",
        typeof(string),
        typeof(WpfLanguageService));

    private static readonly IReadOnlyDictionary<string, string> EnglishToTurkish =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["_File"] = "_Dosya", ["_New"] = "_Yeni", ["_Open…"] = "_Aç…", ["_Save"] = "_Kaydet",
            ["_Print…"] = "_Yazdır…", ["Export as _PNG…"] = "_PNG olarak dışa aktar…",
            ["Export as P_DF…"] = "P_DF olarak dışa aktar…", ["E_xit"] = "Çı_kış",
            ["_Edit"] = "_Düzen", ["_Undo"] = "_Geri al", ["_Redo"] = "_Yinele", ["Cu_t"] = "_Kes",
            ["_Copy"] = "_Kopyala", ["_Paste"] = "_Yapıştır", ["D_uplicate"] = "Ç_oğalt",
            ["Deselect _all"] = "Tüm seçimi _kaldır", ["_Delete selected"] = "Seçileni _sil",
            ["_View"] = "_Görünüm", ["Zoom _in"] = "_Yakınlaştır", ["Zoom _out"] = "_Uzaklaştır",
            ["_Actual size"] = "_Gerçek boyut", ["Show _grid"] = "_Izgarayı göster",
            ["Show _rulers"] = "_Cetvelleri göster", ["_Arrange"] = "_Düzenle",
            ["Bring to _front"] = "En _öne getir", ["Bring _forward"] = "Öne _al",
            ["Send _backward"] = "Arkaya _al", ["Send to _back"] = "En _arkaya gönder",
            ["_Help"] = "_Yardım", ["_User guide"] = "_Kullanım kılavuzu", ["Language"] = "Dil",
            ["English"] = "İngilizce", ["Turkish"] = "Türkçe",
            ["Delete"] = "Sil", ["Print"] = "Yazdır", ["New"] = "Yeni", ["Open"] = "Aç",
            ["Save"] = "Kaydet", ["Undo"] = "Geri al", ["Redo"] = "Yinele", ["Label"] = "Etiket",
            ["Select an element to show formatting controls"] = "Biçimlendirme denetimlerini görmek için bir öğe seçin",
            ["TEXT"] = "METİN", ["SHAPE"] = "ŞEKİL", ["MULTI"] = "ÇOKLU", ["DATA"] = "VERİ",
            ["Stroke"] = "Çizgi", ["Content"] = "İçerik", ["Visible"] = "Görünür",
            ["Lock position"] = "Konumu kilitle", ["Rotate"] = "Döndür", ["Left"] = "Sol",
            ["Center"] = "Orta", ["Right"] = "Sağ", ["Top"] = "Üst", ["Middle"] = "Orta",
            ["Bottom"] = "Alt", ["Space H"] = "Yatay dağıt", ["Space V"] = "Dikey dağıt",
            ["Grid"] = "Izgara", ["Rulers"] = "Cetveller", ["LAYERS · FRONT TO BACK"] = "KATMANLAR · ÖNDEN ARKAYA",
            ["Front"] = "En öne", ["Up"] = "Yukarı", ["Down"] = "Aşağı", ["Back"] = "En arkaya",
            ["Show"] = "Göster", ["Lock"] = "Kilitle",
            ["Print label"] = "Etiketi yazdır", ["Print preview"] = "Baskı önizleme",
            ["PRINT SETTINGS"] = "YAZDIRMA AYARLARI", ["Printer"] = "Yazıcı", ["Copies"] = "Kopya",
            ["No Windows printers are available."] = "Kullanılabilir Windows yazıcısı yok.",
            ["Printer margins (mm)"] = "Yazıcı kenar boşlukları (mm)", ["Calibration offset (mm)"] = "Kalibrasyon kaydırması (mm)",
            ["X · horizontal"] = "X · yatay", ["Y · vertical"] = "Y · dikey",
            ["Document defaults"] = "Belge varsayılanları", ["Cancel"] = "İptal",
            ["Close"] = "Kapat", ["wLabelDesigner Help"] = "wLabelDesigner Yardım",
            ["wLabelDesigner User Guide"] = "wLabelDesigner Kullanım Kılavuzu",
            ["Design and print thermal labels on Windows"] = "Windows'ta termal etiketler tasarlayın ve yazdırın",
            ["Getting started"] = "Başlarken", ["Selecting and editing"] = "Seçme ve düzenleme",
            ["Layers, locking, and visibility"] = "Katmanlar, kilitleme ve görünürlük",
            ["Canvas navigation and alignment guides"] = "Tuval gezintisi ve hizalama kılavuzları",
            ["Keyboard shortcuts"] = "Klavye kısayolları", ["Files and application"] = "Dosyalar ve uygulama",
            ["Editing"] = "Düzenleme", ["Navigation and positioning"] = "Gezinme ve konumlandırma",
            ["Text editing"] = "Metin düzenleme", ["Templates and printing"] = "Şablonlar ve yazdırma",
            ["About"] = "Hakkında",
            ["Create a label, set its physical width, height, and printer DPI in the top bar, then drag tools from the left rail onto the white label. Move elements directly, resize them with their handles, and use the contextual top bar for text or shape formatting."] =
                "Bir etiket oluşturun; üst çubuktan fiziksel genişlik, yükseklik ve yazıcı DPI değerini ayarlayın. Ardından sol araç çubuğundaki araçları beyaz etiketin üzerine sürükleyin. Öğeleri doğrudan taşıyabilir, tutamaçlarla boyutlandırabilir ve metin ya da şekil biçimlendirmesi için bağlamsal üst çubuğu kullanabilirsiniz.",
            ["Click an element to select it. Shift-click toggles additional elements in the selection. Click empty workspace or press Escape to clear the selection. Drag a selected element to move the whole selection. Use the eight handles to resize regular elements; lines use two endpoint handles."] =
                "Bir öğeyi seçmek için tıklayın. Shift+tıklama ek öğeleri seçime ekler veya çıkarır. Seçimi temizlemek için boş çalışma alanına tıklayın ya da Escape tuşuna basın. Tüm seçimi taşımak için seçili bir öğeyi sürükleyin. Normal öğeleri sekiz tutamaçla, çizgileri iki uç tutamacıyla boyutlandırın.",
            ["Double-click text to edit it directly. Enter creates a new line, Ctrl+Enter commits, Escape cancels, and clicking away commits. Barcode and QR data is edited in the contextual panel on the right."] =
                "Metni doğrudan düzenlemek için çift tıklayın. Enter yeni satır oluşturur, Ctrl+Enter değişikliği uygular, Escape iptal eder; başka bir yere tıklamak değişikliği uygular. Barkod ve QR verileri bağlamsal üst çubukta düzenlenir.",
            ["The Layers panel lists elements from front to back. Select a layer, then use Front, Up, Down, or Back to change its stacking order. The same commands are available from the Arrange menu."] =
                "Katmanlar paneli öğeleri önden arkaya listeler. Bir katman seçin ve sıralamasını değiştirmek için En öne, Yukarı, Aşağı veya En arkaya düğmelerini kullanın. Aynı komutlar Düzenle menüsünde de bulunur.",
            ["Lock an element to prevent its position from changing. A position-locked element can still be selected, resized, rotated, formatted, deleted, and reordered in the Layers panel. Hidden elements remain available in the Layers panel but do not appear on the label, in print preview, or in printed output."] =
                "Konumunun değişmesini önlemek için bir öğeyi kilitleyin. Konumu kilitli bir öğe yine de seçilebilir, boyutlandırılabilir, döndürülebilir, biçimlendirilebilir, silinebilir ve Katmanlar panelinde yeniden sıralanabilir. Gizli öğeler Katmanlar panelinde kalır ancak etikette, baskı önizlemede veya basılı çıktıda görünmez.",
            ["Templates use the .fckbartndr extension and JSON data. They store label size, DPI, all elements and styles, printer choice, and default copy count. Print opens a preview that uses the same renderer as physical output. Choose a printer and copies, then press Print. Save the document afterward when the title shows an asterisk."] =
                "Şablonlar .fckbartndr uzantısını ve JSON verisini kullanır. Etiket boyutunu, DPI değerini, tüm öğe ve stilleri, yazıcı seçimini ve varsayılan kopya sayısını saklar. Yazdır komutu fiziksel çıktıyla aynı işleyiciyi kullanan bir önizleme açar. Yazıcıyı ve kopya sayısını seçip Yazdır'a basın. Başlıkta yıldız göründüğünde belgeyi kaydedin.",
            ["wLabelDesigner is a lightweight .NET 10 WPF application for designing and printing thermal labels, optimized for common 203 and 300 DPI Windows label printers."] =
                "wLabelDesigner, termal etiket tasarlamak ve yazdırmak için geliştirilmiş; yaygın 203 ve 300 DPI Windows etiket yazıcıları için iyileştirilmiş hafif bir .NET 10 WPF uygulamasıdır.",
            ["Text"] = "Metin", ["Barcode"] = "Barkod", ["QR code"] = "QR kodu", ["Box"] = "Kutu",
            ["Rounded box"] = "Yuvarlatılmış kutu", ["Line"] = "Çizgi", ["Image"] = "Görsel",
            ["Ready"] = "Hazır", ["Selection cleared"] = "Seçim temizlendi", ["Action cancelled"] = "İşlem iptal edildi",
            ["Save cancelled"] = "Kaydetme iptal edildi", ["Printing cancelled"] = "Yazdırma iptal edildi",
            ["Image import cancelled"] = "Görsel içe aktarma iptal edildi", ["PNG export cancelled"] = "PNG dışa aktarma iptal edildi",
            ["PDF export cancelled"] = "PDF dışa aktarma iptal edildi",
            ["New 100 × 50 mm label"] = "Yeni 100 × 50 mm etiket",
            ["Element deleted"] = "Öğe silindi", ["Element copied"] = "Öğe kopyalandı", ["Element cut"] = "Öğe kesildi",
            ["Label sent to printer"] = "Etiket yazıcıya gönderildi",
            ["Image added"] = "Görsel eklendi", ["Text added"] = "Metin eklendi", ["Barcode added"] = "Barkod eklendi",
            ["QR code added"] = "QR kodu eklendi", ["Box added"] = "Kutu eklendi", ["Rounded box added"] = "Yuvarlatılmış kutu eklendi",
            ["Line added"] = "Çizgi eklendi"
        };

    private readonly IReadOnlyDictionary<string, string> turkishToEnglish =
        EnglishToTurkish
            .GroupBy(pair => pair.Value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.Ordinal);
    private string currentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? "tr" : "en";

    public static WpfLanguageService Instance { get; } = new();

    private WpfLanguageService()
    {
    }

    public string CurrentLanguage => currentLanguage;

    public bool IsTurkish => currentLanguage == "tr";

    public event EventHandler? LanguageChanged;

    public void SetLanguage(string language)
    {
        var normalized = string.Equals(language, "tr", StringComparison.OrdinalIgnoreCase) ? "tr" : "en";
        if (normalized == currentLanguage)
        {
            return;
        }

        currentLanguage = normalized;
        var culture = CultureInfo.GetCultureInfo(normalized == "tr" ? "tr-TR" : "en-US");
        CultureInfo.CurrentUICulture = culture;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Translate(string text)
    {
        var translations = IsTurkish ? EnglishToTurkish : turkishToEnglish;
        if (translations.TryGetValue(text, out var translated))
        {
            return translated;
        }

        if (!IsTurkish)
        {
            return text;
        }

        return text switch
        {
            _ when text.StartsWith("Opened ", StringComparison.Ordinal) => $"{text[7..]} açıldı",
            _ when text.StartsWith("Saved ", StringComparison.Ordinal) => $"{text[6..]} kaydedildi",
            _ when text.StartsWith("Selected ", StringComparison.Ordinal) => $"{Translate(text[9..])} seçildi",
            _ when text.StartsWith("Zoom ", StringComparison.Ordinal) => $"Yakınlaştırma {text[5..]}",
            _ when text.StartsWith("PNG exported to ", StringComparison.Ordinal) => $"PNG {text[16..]} dosyasına aktarıldı",
            _ when text.StartsWith("PDF exported to ", StringComparison.Ordinal) => $"PDF {text[16..]} dosyasına aktarıldı",
            _ when text.StartsWith("Could not ", StringComparison.Ordinal) => $"İşlem başarısız: {text[10..]}",
            _ => text
        };
    }

    public static string TranslateForLanguage(string text, string language) =>
        string.Equals(language, "tr", StringComparison.OrdinalIgnoreCase)
            ? EnglishToTurkish.GetValueOrDefault(text, text)
            : text;

    public void Apply(DependencyObject root)
    {
        var children = LogicalTreeHelper
            .GetChildren(root)
            .OfType<DependencyObject>()
            .ToArray();

        TranslateProperty(root, root switch
        {
            TextBlock => TextBlock.TextProperty,
            Run => Run.TextProperty,
            HeaderedItemsControl => HeaderedItemsControl.HeaderProperty,
            Window => Window.TitleProperty,
            ContentControl => ContentControl.ContentProperty,
            _ => null
        });
        TranslateProperty(root, ToolTipService.ToolTipProperty);

        foreach (var child in children)
        {
            Apply(child);
        }
    }

    private void TranslateProperty(DependencyObject target, DependencyProperty? property)
    {
        if (property is null)
        {
            return;
        }

        var originalProperty = property == ToolTipService.ToolTipProperty
            ? OriginalToolTipProperty
            : OriginalTextProperty;
        var original = target.GetValue(originalProperty) as string;
        var localValue = target.ReadLocalValue(property);
        if (original is null && localValue is string source)
        {
            original = source;
            target.SetValue(originalProperty, original);
        }

        if (original is not null)
        {
            target.SetCurrentValue(property, IsTurkish
                ? EnglishToTurkish.GetValueOrDefault(original, original)
                : original);
        }
    }
}
