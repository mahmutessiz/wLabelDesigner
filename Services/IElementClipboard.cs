using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public interface IElementClipboard
{
    bool ContainsElement();

    bool TryCopy(LabelElementData element);

    bool TryGetElement(out LabelElementData? element);
}
