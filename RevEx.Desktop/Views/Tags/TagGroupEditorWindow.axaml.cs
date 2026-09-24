using Avalonia.Controls;

namespace RevEx.Desktop.Views.Tags;

public enum EditorAction { Save, Delete }
public sealed record TagGroupEditorResult(EditorAction Action, string Name);

public partial class TagGroupEditorWindow : Window
{
    public string EntityName { get; set; }
    public bool IsEditing { get; }
    public string DialogTitle => IsEditing ? "Редактирование группы" : "Новая группа";

    public TagGroupEditorWindow() : this(null) { }

    public TagGroupEditorWindow(string? name)
    {
        EntityName = name ?? string.Empty;
        IsEditing = name is not null;
        InitializeComponent();
        DataContext = this;
        Opened += (_, _) => NameBox.Focus();
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) { ValidationMessage.Text = "Введите название группы."; return; }
        Close(new TagGroupEditorResult(EditorAction.Save, name));
    }

    private void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) =>
        Close(new TagGroupEditorResult(EditorAction.Delete, EntityName));

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close(null);
}
