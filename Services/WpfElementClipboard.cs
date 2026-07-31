using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public sealed class WpfElementClipboard : IElementClipboard
{
    private const string ClipboardFormat = "wLabelDesigner.LabelElement.v1";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public bool ContainsElement()
    {
        try
        {
            return Clipboard.ContainsData(ClipboardFormat);
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    public bool TryCopy(LabelElementData element)
    {
        ArgumentNullException.ThrowIfNull(element);

        try
        {
            var data = new DataObject();
            data.SetData(ClipboardFormat, JsonSerializer.Serialize(element, SerializerOptions));
            Clipboard.SetDataObject(data, copy: true);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    public bool TryGetElement(out LabelElementData? element)
    {
        element = null;
        try
        {
            if (Clipboard.GetData(ClipboardFormat) is not string json)
            {
                return false;
            }

            element = JsonSerializer.Deserialize<LabelElementData>(json, SerializerOptions);
            return element is not null;
        }
        catch (Exception exception) when (exception is ExternalException or JsonException)
        {
            return false;
        }
    }
}
