using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public interface ILabelDocumentStore
{
    Task<LabelDocument> LoadAsync(string path, CancellationToken cancellationToken = default);

    Task SaveAsync(string path, LabelDocument document, CancellationToken cancellationToken = default);
}
