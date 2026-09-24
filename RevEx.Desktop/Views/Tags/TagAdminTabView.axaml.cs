using System.Linq;
using Avalonia.Controls;
using RevEx.Desktop.ViewModels.Tags;

namespace RevEx.Desktop.Views.Tags;

public partial class TagAdminTabView : UserControl
{
    public TagAdminTabView() => InitializeComponent();

    private async void OnCreateGroupClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!TryGetContext(out var owner, out var viewModel)) return;
        var result = await new TagGroupEditorWindow().ShowDialog<TagGroupEditorResult?>(owner);
        if (result is { Action: EditorAction.Save })
            await viewModel.AddGroupFromDialogAsync(result.Name);
    }

    private async void OnEditGroupClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: TagGroupItemViewModel group } ||
            !TryGetContext(out var owner, out var viewModel)) return;
        var result = await new TagGroupEditorWindow(group.Name).ShowDialog<TagGroupEditorResult?>(owner);
        if (result is null) return;
        if (result.Action == EditorAction.Delete)
            await viewModel.DeleteGroupFromDialogAsync(group);
        else
            await viewModel.EditGroupFromDialogAsync(group, result.Name);
    }

    private async void OnDeleteGroupClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: TagGroupItemViewModel group } ||
            !TryGetContext(out var owner, out var viewModel)) return;
        var message = group.Tags.Count == 0
            ? $"Группа «{group.Name}» будет удалена."
            : $"В группе «{group.Name}» есть теги. Сначала перенесите или удалите их.";
        if (group.Tags.Count > 0)
        {
            await new DeleteConfirmationWindow(message, false).ShowDialog<bool>(owner);
            return;
        }
        if (await new DeleteConfirmationWindow(message).ShowDialog<bool>(owner))
            await viewModel.DeleteGroupFromDialogAsync(group);
    }

    private async void OnCreateTagClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!TryGetContext(out var owner, out var viewModel) || viewModel.Groups.Count == 0) return;
        var result = await new TagEditorWindow(viewModel.Groups, null, viewModel.Groups.First())
            .ShowDialog<TagEditorResult?>(owner);
        if (result is { Action: EditorAction.Save })
            await viewModel.AddTagFromDialogAsync(result.Name, result.Group);
    }

    private async void OnEditTagClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: TagItemViewModel tag } ||
            !TryGetContext(out var owner, out var viewModel)) return;
        var group = viewModel.Groups.First(item => item.Id == tag.TagGroupId);
        var result = await new TagEditorWindow(viewModel.Groups, tag.Name, group)
            .ShowDialog<TagEditorResult?>(owner);
        if (result is null) return;
        if (result.Action == EditorAction.Delete)
            await viewModel.DeleteTagFromDialogAsync(tag);
        else
            await viewModel.EditTagFromDialogAsync(tag, result.Name, result.Group);
    }

    private async void OnDeleteTagClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: TagItemViewModel tag } ||
            !TryGetContext(out var owner, out var viewModel)) return;
        var message = $"Тег «{tag.Name}» будет удалён и снят со всех материалов.";
        if (await new DeleteConfirmationWindow(message).ShowDialog<bool>(owner))
            await viewModel.DeleteTagFromDialogAsync(tag);
    }

    private bool TryGetContext(out Window owner, out TagAdminTabViewModel viewModel)
    {
        owner = null!;
        viewModel = null!;
        if (DataContext is not TagAdminTabViewModel currentViewModel ||
            TopLevel.GetTopLevel(this) is not Window currentOwner) return false;
        owner = currentOwner;
        viewModel = currentViewModel;
        return true;
    }
}
