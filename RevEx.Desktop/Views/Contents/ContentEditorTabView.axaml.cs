using Avalonia.Controls;
using Avalonia.Interactivity;
using RevEx.Desktop.Configuration;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Common;
using RevEx.Desktop.Views.Tags;
using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentEditorTabView : UserControl
{
    public ContentEditorTabView()
    {
        InitializeComponent();
    }

    private async void OnChooseFileClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentEditorTabViewModel viewModel)
        {
            return;
        }

        var file = await LocalFilePicker.OpenAsync(this, "Выберите Revit-семейство", LocalFilePicker.RevitFamily);
        if (file is not null)
        {
            viewModel.SetSelectedFamilyFile(file.Path, file.Name);
        }
    }

    private async void OnChoosePreviewClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentEditorTabViewModel viewModel)
        {
            return;
        }

        var file = await LocalFilePicker.OpenAsync(this, "Выберите изображение контента", ContentImageUploadConfiguration.FilePickerType);
        if (file is not null)
        {
            viewModel.SetSelectedPreview(file.Path, file.Name);
        }
    }

    private async void OnAddTagClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentEditorTabViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var tag = await new TagSelectionWindow(viewModel.GetAvailableTags()).ShowDialog<TagDto?>(owner);
        if (tag is not null)
        {
            viewModel.AddTag(tag);
        }
    }
}
