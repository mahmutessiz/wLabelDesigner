namespace wLabelDesigner.ViewModels;

public sealed record SelectionOption<T>(T Value, string Name) where T : struct, Enum;
