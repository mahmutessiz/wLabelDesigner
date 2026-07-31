using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public interface ILabelPrintService
{
    bool Print(LabelDocument document);
}
