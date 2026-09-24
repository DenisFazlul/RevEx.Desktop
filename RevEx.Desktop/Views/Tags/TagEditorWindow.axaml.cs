using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using RevEx.Desktop.ViewModels.Tags;

namespace RevEx.Desktop.Views.Tags;

public sealed record TagEditorResult(EditorAction Action, string Name, TagGroupItemViewModel Group);

public partial class TagEditorWindow : Window
{
    public string EntityName { get; set; }
    public IReadOnlyCollection<TagGroupItemViewModel> Groups { get; }
    public TagGroupItemViewModel? SelectedGroup { get; set; }
    public bool IsEditing { get; }
    public string DialogTitle => IsEditing ? "Редактирование тега" : "Новый тег";

    public TagEditorWindow() : this([], null, null) { }

    public TagEditorWindow(
        IEnumerable<TagGroupItemViewModel> groups,
        string? name,
        TagGroupItemViewModel? selectedGroup)
    {
        Groups = groups.ToArray();
        EntityName = name ?? string.Empty;
        SelectedGroup = selectedGroup ?? Groups.FirstOrDefault();
        IsEditing = name is not null;
        InitializeComponent();
        DataContext = this;
        Opened += (_, _) => NameBox.Focus();
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) { ValidationMessage.Text = "Введите название тега."; return; }
        if (GroupBox.SelectedItem is not TagGroupItemViewModel group)
        { ValidationMessage.Text = "Выберите группу."; return; }
        Close(new TagEditorResult(EditorAction.Save, name, group));
    }

    private void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (GroupBox.SelectedItem is TagGroupItemViewModel group)
            Close(new TagEditorResult(EditorAction.Delete, EntityName, group));
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close(null);
}
