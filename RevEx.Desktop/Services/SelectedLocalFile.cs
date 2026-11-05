namespace RevEx.Desktop.Services;

/// <summary>Результат выбора локального файла: путь и имя без зависимости от API диалогов.</summary>
public sealed record SelectedLocalFile(string Path, string Name);
