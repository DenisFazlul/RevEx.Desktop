using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.ViewModels.Common;

namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Модель редактора тега. Проверяет название и группу, затем возвращает выбранное действие.</summary>
public partial class TagEditorViewModel : TextEditorViewModel<TagEditorResult>
{
    [ObservableProperty]
    private TagGroupItemViewModel? _selectedGroup;

    public TagEditorViewModel(IEnumerable<TagGroupItemViewModel> groups, string? name, TagGroupItemViewModel? selectedGroup)
    {
        Groups = groups.ToArray();
        Text = name ?? string.Empty;
        IsEditing = name is not null;
        SelectedGroup = selectedGroup ?? Groups.FirstOrDefault();
    }

    public IReadOnlyList<TagGroupItemViewModel> Groups
    {
        get;
    }
    public bool IsEditing
    {
        get;
    }
    public string DialogTitle => IsEditing ? "Редактирование тега" : "Новый тег";

    [RelayCommand]
    private void Save()
    {
        if (!TryReadText("Введите название тега.", 100, out var name))
        {
            return;
        }

        if (SelectedGroup is null)
        {
            ValidationMessage = "Выберите группу.";
            return;
        }

        Complete(new TagEditorResult(EditorAction.Save, name, SelectedGroup));
    }

    [RelayCommand]
    private void Delete()
    {
        if (IsEditing && SelectedGroup is not null)
        {
            Complete(new TagEditorResult(EditorAction.Delete, Text, SelectedGroup));
        }
    }
}
