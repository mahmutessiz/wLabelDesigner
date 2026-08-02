using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public interface ILabelExportService
{
    Task<string?> ExportPngAsync(LabelDocument document, CancellationToken cancellationToken = default);
}
