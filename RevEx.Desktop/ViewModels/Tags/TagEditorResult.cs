namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Результат редактирования тега: действие, название и выбранная группа.</summary>
public sealed record TagEditorResult(EditorAction Action, string Name, TagGroupItemViewModel Group);
