using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using RevEx.Desktop.Configuration;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Categories;
using RevEx.Desktop.Views.Common;
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

    private async void OnEditNameClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var editor = new TextInputWindow(new TextInputOptions(
            "Редактирование названия",
            "Название контента",
            "Название",
            "Введите новое название карточки контента",
            viewModel.Name,
            MaximumLength: 200));
        var name = await editor.ShowDialog<string?>(owner);
        if (name is not null)
            await viewModel.UpdateContentNameAsync(name);
    }

    private async void OnEditDescriptionClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var editor = new TextInputWindow(new TextInputOptions(
            "Редактирование описания",
            "Описание контента",
            "Описание",
            "Введите новое описание карточки контента",
            viewModel.Description,
            IsMultiline: true));
        var description = await editor.ShowDialog<string?>(owner);
        if (description is not null)
            await viewModel.UpdateContentDescriptionAsync(description);
    }

    private void OnVersionDoubleTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (DataContext is ContentDetailsViewModel viewModel &&
            sender is Control { DataContext: ContentVersionDto version })
            viewModel.OpenVersion(version);
    }

    private async void OnEditCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var selector = new CategorySelectionWindow(viewModel.GetCategories(), viewModel.GetCurrentCategoryId());
        var category = await selector.ShowDialog<CategoryDto?>(owner);
        if (category is not null)
            await viewModel.UpdateContentCategoryAsync(category);
    }
}
