using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.ViewModels.Common;

namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Модель редактора группы тегов. Проверяет название и возвращает сохранение или удаление.</summary>
public partial class TagGroupEditorViewModel : TextEditorViewModel<TagGroupEditorResult>
{
    public TagGroupEditorViewModel(string? name)
    {
        Text = name ?? string.Empty;
        IsEditing = name is not null;
    }

    public bool IsEditing
    {
        get;
    }
    public string DialogTitle => IsEditing ? "Редактирование группы" : "Новая группа";

    [RelayCommand]
    private void Save()
    {
        if (TryReadText("Введите название группы.", 100, out var name))
        {
            Complete(new TagGroupEditorResult(EditorAction.Save, name));
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (IsEditing)
        {
            Complete(new TagGroupEditorResult(EditorAction.Delete, Text));
        }
    }
}
