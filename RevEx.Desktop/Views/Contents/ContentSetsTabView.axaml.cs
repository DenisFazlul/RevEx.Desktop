using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels.Contents;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentSetsTabView : UserControl
{
    public ContentSetsTabView()
    {
        InitializeComponent();
    }

    private async void OnCreateClick(object? sender, RoutedEventArgs eventArgs)
    {
        await ShowEditorAsync(true);
    }

    private async void OnEditClick(object? sender, RoutedEventArgs eventArgs)
    {
        await ShowEditorAsync(false);
    }

    private async Task ShowEditorAsync(bool isNew)
    {
        if (DataContext is not ContentSetsTabViewModel model ||
            TopLevel.GetTopLevel(this) is not Window owner || (!isNew && !model.HasSelectedSet))
        {
            return;
        }

        var window = new ContentSetEditorWindow(model.CreateEditor(isNew));
        var id = await window.ShowDialog<int?>(owner);
        if (id.HasValue)
        {
            model.Search = string.Empty;
            await model.RefreshAsync(id.Value);
        }
    }

    private void OnContentDoubleTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (DataContext is ContentSetsTabViewModel model &&
            sender is Control { DataContext: ContentItemViewModel content })
        {
            model.OpenContent(content);
        }
    }
}
