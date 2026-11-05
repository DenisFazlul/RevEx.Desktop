namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Результат редактирования группы: действие пользователя и название.</summary>
public sealed record TagGroupEditorResult(EditorAction Action, string Name);
