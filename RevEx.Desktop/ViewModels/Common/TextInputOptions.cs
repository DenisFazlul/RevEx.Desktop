namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Настройки текстовой формы: подписи, начальное значение и ограничения ввода.</summary>
public sealed record TextInputOptions(
    string WindowTitle,
    string FormTitle,
    string FieldLabel,
    string Hint,
    string InitialValue,
    bool IsMultiline = false,
    int MaximumLength = 5000);
