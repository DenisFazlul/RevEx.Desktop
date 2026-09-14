using Avalonia.Controls;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Tags;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentDetailsView : UserControl
{
    public ContentDetailsView()
    {
        InitializeComponent();
    }

    private async void OnAddTagClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var availableTags = await viewModel.GetAvailableTagsAsync();
        var window = new TagSelectionWindow(availableTags);
        var tag = await window.ShowDialog<TagDto?>(owner);
        if (tag is not null)
            await viewModel.AssignTagAsync(tag);
    }
}
