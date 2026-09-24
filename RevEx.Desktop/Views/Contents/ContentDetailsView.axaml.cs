using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using RevEx.Desktop.Configuration;
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

    private async void OnUpdatePreviewClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null || DataContext is not ContentDetailsViewModel viewModel)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите новое изображение контента",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { ContentImageUploadConfiguration.FilePickerType }
        });
        var file = files.FirstOrDefault();
        if (file is not null)
            await viewModel.UpdatePreviewAsync(file.Path.LocalPath, file.Name);
    }

    private async void OnDeleteContentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var confirmation = new DeleteConfirmationWindow(
            $"Контент «{viewModel.Name}» и все его версии будут удалены. Продолжить?");
        if (await confirmation.ShowDialog<bool>(owner))
            await viewModel.DeleteContentAsync();
    }
}
