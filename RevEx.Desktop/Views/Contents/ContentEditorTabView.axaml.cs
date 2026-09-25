using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RevEx.Desktop.Configuration;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Tags;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentEditorTabView : UserControl
{
    private static readonly FilePickerFileType RevitFamilyFileType = new("Revit Family")
    {
        Patterns = ["*.rfa"]
    };
    public ContentEditorTabView() => InitializeComponent();

    private async void OnChooseFileClick(object? sender, RoutedEventArgs eventArgs)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите Revit-семейство",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { RevitFamilyFileType }
        });
        var file = files.FirstOrDefault();
        if (file is null || DataContext is not ContentEditorTabViewModel viewModel)
            return;

        viewModel.SetSelectedFamilyFile(file.Path.LocalPath, file.Name);
    }

    private async void OnChoosePreviewClick(object? sender, RoutedEventArgs eventArgs)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите изображение контента",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { ContentImageUploadConfiguration.FilePickerType }
        });
        var file = files.FirstOrDefault();
        if (file is not null && DataContext is ContentEditorTabViewModel viewModel)
            viewModel.SetSelectedPreview(file.Path.LocalPath, file.Name);
    }

    private async void OnAddTagClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentEditorTabViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var selector = new TagSelectionWindow(viewModel.GetAvailableTags());
        var tag = await selector.ShowDialog<TagDto?>(owner);
        if (tag is not null)
            viewModel.AddTag(tag);
    }
}
