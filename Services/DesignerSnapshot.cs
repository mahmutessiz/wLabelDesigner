using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public sealed record DesignerSnapshot(LabelDocument Document, Guid? SelectedElementId);
